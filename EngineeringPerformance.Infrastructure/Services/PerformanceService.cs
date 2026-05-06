using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class PerformanceService : IPerformanceService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PerformanceService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(int projectId, int versionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var versionExists = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .AnyAsync(v => v.Id == versionId && v.ProjectId == projectId);

        var workDirections = await context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToListAsync();

        var physicalVolumes = await context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Include(pv => pv.PhysicalVolume)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var laborIntensities = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        // V2-B.1: нормы берутся из снимка версии, а не runtime-фильтром по EffectiveFrom/EffectiveTo.
        // Это фиксирует Z-score-отчёт: правки справочника норм после создания версии больше не «плывут» по истории.
        var snapshotNorms = versionExists
            ? await context.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Include(s => s.Norm)
                .Select(s => s.Norm)
                .ToListAsync()
            : new List<IicWorkDirectionNorm>();

        var directionById = workDirections.ToDictionary(d => d.Id);
        var parentIdLookup = workDirections.Where(d => d.ParentId.HasValue).ToLookup(d => d.ParentId!.Value);

        var volumeByDirection = physicalVolumes
            .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true && pv.PhysicalVolume.IsAssessment == true)
            .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));

        // В Excel часто заполняют только «план» (PlannedHours); для таблицы производительности
        // берём факт, а при нулевом факте — план по строке (как в ProjectForecastService для часов).
        // Строки без привязки к направлению IIC (WorkDirectionId = NULL) пропускаем — для них
        // производительность в разрезе направлений неопределена.
        var hoursByDirection = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(li => li.ActualHours > 0 ? li.ActualHours : li.PlannedHours));

        // Снимок гарантирует уникальность (ProjectAnalysisVersionId, WorkDirectionId)
        // через UX_AnalysisVersionSnapshots_Version_WorkDirection — никакого недетерминизма
        // и группировки «бери самую свежую» больше не нужно.
        var normByDirection = snapshotNorms
            .ToDictionary(n => n.WorkDirectionId);

        var rootDirections = workDirections.Where(d => !d.ParentId.HasValue).ToList();
        var result = new List<PerformanceRowDto>();
        var visitedDirections = new HashSet<int>();

        foreach (var root in rootDirections)
        {
            BuildHierarchy(root, 0, parentIdLookup, volumeByDirection, hoursByDirection, normByDirection, directionById, result, visitedDirections);
        }

        var unallocatedPhysicalVolume = physicalVolumes
            .Where(pv => pv.PhysicalVolume is not null && !pv.PhysicalVolume.ClassifierId.HasValue && pv.PhysicalVolume.IsAssessment == true)
            .Sum(pv => pv.QuantityInIicGroupUnits());

        result.Add(new PerformanceRowDto(
            WorkDirectionId: PerformanceRowDto.UnallocatedPhysicalVolumeWorkDirectionId,
            WorkDirectionName: "(пусто)",
            WorkDirectionCode: string.Empty,
            ParentId: null,
            Level: -1,
            NormUnitName: null,
            FactPhysicalVolume: unallocatedPhysicalVolume,
            FactLaborHours: null,
            FactProductivity: null,
            FactEstimate: null,
            NormProductivity: null,
            NormEstimate: null,
            NormStdDev: null,
            ZScore: null,
            Interpretation: null));

        return result;
    }

    /// <summary>
    /// Рекурсивно строит иерархию направлений, добавляя в <paramref name="result"/> строки в pre-order.
    /// Возвращает агрегированные по поддереву (часы, сумма «оценка × часы»), используемые для
    /// средне-взвешенной фактической оценки родителей (вес = трудозатраты элемента).
    /// </summary>
    private static (decimal SubtreeHours, decimal SubtreeWeightedEstimate) BuildHierarchy(
        IicWorkDirection direction,
        int level,
        ILookup<int, IicWorkDirection> parentIdLookup,
        IReadOnlyDictionary<int, decimal> volumeByDirection,
        IReadOnlyDictionary<int, decimal> hoursByDirection,
        IReadOnlyDictionary<int, IicWorkDirectionNorm> normByDirection,
        IReadOnlyDictionary<int, IicWorkDirection> directionById,
        List<PerformanceRowDto> result,
        HashSet<int> visited)
    {
        // Защита от циклов: UI-валидатор их блокирует, но через прямую правку БД
        // цикл всё же может попасть в данные. Без guard рекурсия упадёт StackOverflow,
        // что нельзя поймать catch — крэш всего приложения.
        if (!visited.Add(direction.Id))
        {
            return (0m, 0m);
        }

        var factVolume = volumeByDirection.GetValueOrDefault(direction.Id);
        var factHours = hoursByDirection.GetValueOrDefault(direction.Id);

        decimal? factProductivity = null;
        if (factVolume > 0)
        {
            factProductivity = factHours / factVolume;
        }

        decimal? ownFactEstimate = null;
        if (factProductivity.HasValue && factProductivity.Value > 0)
        {
            ownFactEstimate = (decimal)Math.Log((double)factProductivity.Value);
        }

        // Собственный вклад узла в средне-взвешенную оценку поддерева:
        // включаем только когда оценка по узлу определена и вес (часы) положительный.
        decimal subtreeHours = (ownFactEstimate.HasValue && factHours > 0) ? factHours : 0m;
        decimal subtreeWeightedEstimate = (ownFactEstimate.HasValue && factHours > 0)
            ? ownFactEstimate.Value * factHours
            : 0m;

        // Резервируем место под текущую строку, чтобы сохранить pre-order
        // (родитель идёт раньше детей в результирующем списке).
        var rowIndex = result.Count;
        result.Add(null!);

        var children = parentIdLookup[direction.Id];
        foreach (var child in children)
        {
            var (childHours, childWeighted) = BuildHierarchy(
                child,
                level + 1,
                parentIdLookup,
                volumeByDirection,
                hoursByDirection,
                normByDirection,
                directionById,
                result,
                visited);
            subtreeHours += childHours;
            subtreeWeightedEstimate += childWeighted;
        }

        // Агрегированная фактическая оценка по узлу: средне-взвешенное по часам.
        // Для листьев совпадает с собственной оценкой; для родителей — учитывает потомков.
        decimal? aggregatedFactEstimate = subtreeHours > 0
            ? subtreeWeightedEstimate / subtreeHours
            : ownFactEstimate;

        normByDirection.TryGetValue(direction.Id, out var norm);

        decimal? normProductivity = norm?.Performance;
        decimal? normEstimate = norm?.Estimate;
        decimal? normStdDev = norm?.StandardDeviation;
        var normUnitName = IicWorkDirectionDisplayUnit.ResolveUnitName(direction, directionById);

        decimal? zScore = null;
        if (aggregatedFactEstimate.HasValue && normEstimate.HasValue && normStdDev.HasValue && normStdDev.Value > 0)
        {
            zScore = (aggregatedFactEstimate.Value - normEstimate.Value) / normStdDev.Value;
        }

        PerformanceInterpretation? interpretation = zScore.HasValue ? DetermineInterpretation(zScore.Value) : null;

        result[rowIndex] = new PerformanceRowDto(
            WorkDirectionId: direction.Id,
            WorkDirectionName: direction.Name,
            WorkDirectionCode: direction.Code,
            ParentId: direction.ParentId,
            Level: level,
            NormUnitName: normUnitName,
            FactPhysicalVolume: factVolume > 0 ? factVolume : null,
            FactLaborHours: factHours > 0 ? factHours : null,
            FactProductivity: factProductivity,
            FactEstimate: aggregatedFactEstimate,
            NormProductivity: normProductivity,
            NormEstimate: normEstimate,
            NormStdDev: normStdDev,
            ZScore: zScore,
            Interpretation: interpretation);

        return (subtreeHours, subtreeWeightedEstimate);
    }

    private static PerformanceInterpretation DetermineInterpretation(decimal zScore)
    {
        if (zScore < -1.0m)
            return PerformanceInterpretation.Excellent;
        if (zScore < -0.5m)
            return PerformanceInterpretation.Good;
        if (zScore <= 0.5m)
            return PerformanceInterpretation.Satisfactory;
        if (zScore <= 1.0m)
            return PerformanceInterpretation.NeedsAttention;
        return PerformanceInterpretation.Critical;
    }
}
