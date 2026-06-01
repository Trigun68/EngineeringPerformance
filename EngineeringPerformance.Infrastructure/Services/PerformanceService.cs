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
    private List<IicWorkDirection>? _workDirectionsCache;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public PerformanceService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public void InvalidateWorkDirectionHierarchyCache() => _workDirectionsCache = null;

    private async Task<List<IicWorkDirection>> GetWorkDirectionsAsync()
    {
        if (_workDirectionsCache is not null)
            return _workDirectionsCache;

        await _cacheLock.WaitAsync();
        try
        {
            if (_workDirectionsCache is not null)
                return _workDirectionsCache;

            await using var ctx = await _contextFactory.CreateDbContextAsync();
            _workDirectionsCache = await ctx.IicWorkDirections
                .AsNoTracking()
                .Include(d => d.Unit)
                .Where(d => !d.IsArchived)
                .ToListAsync();
            return _workDirectionsCache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(int projectId, int versionId)
    {
        // Запускаем независимые запросы параллельно через отдельные контексты
        await using var ctx1 = await _contextFactory.CreateDbContextAsync();
        await using var ctx3 = await _contextFactory.CreateDbContextAsync();
        await using var ctx4 = await _contextFactory.CreateDbContextAsync();
        await using var ctxEquip = await _contextFactory.CreateDbContextAsync();

        var workDirectionsTask = GetWorkDirectionsAsync();

        var versionExistsTask = ctx1.ProjectAnalysisVersions
            .AsNoTracking()
            .AnyAsync(v => v.Id == versionId && v.ProjectId == projectId);

        var physicalVolumesTask = ctx3.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(pv => pv.PhysicalVolume)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var equipmentTask = ctxEquip.ProjectEquipments
            .AsNoTracking()
            .Include(e => e.Equipment)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var laborIntensitiesTask = ctx4.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        await Task.WhenAll(workDirectionsTask, versionExistsTask, physicalVolumesTask, equipmentTask, laborIntensitiesTask);

        var workDirections = workDirectionsTask.Result;
        var versionExists = versionExistsTask.Result;
        var physicalVolumes = physicalVolumesTask.Result;
        var equipmentRows = equipmentTask.Result;
        var laborIntensities = laborIntensitiesTask.Result;

        IReadOnlyList<AnalysisVersionSnapshot> snapshots;
        if (versionExists)
        {
            await using var ctx5 = await _contextFactory.CreateDbContextAsync();
            snapshots = await ctx5.AnalysisVersionSnapshots
                .AsNoTracking()
                .Where(s => s.ProjectAnalysisVersionId == versionId)
                .Include(s => s.Norm)
                .ToListAsync();
        }
        else
        {
            snapshots = Array.Empty<AnalysisVersionSnapshot>();
        }

        var directionById = workDirections.ToDictionary(d => d.Id);
        var parentIdLookup = workDirections.Where(d => d.ParentId.HasValue).ToLookup(d => d.ParentId!.Value);

        var volumeByDirection = physicalVolumes
            .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true && pv.PhysicalVolume.IsAssessment == true)
            .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));

        EquipmentVolumeAggregator.MergeInto(volumeByDirection, equipmentRows);

        // Единый источник часов для агрегации производительности — PlannedHours
        // (заявленные в файле плановые часы). Прежняя смесь `Actual > 0 ? Actual : Planned`
        // давала итог больше файла на строках, где факт списан > плана (для проекта 4 v2 это
        // было +755.84 ч к 61 725.40). Решено пользователем 2026-05-12 после фикса ISS-006:
        // фактическое списание (`ActualHours`) для агрегации не используется.
        // Строки без привязки к направлению IIC (WorkDirectionId = NULL) пропускаем — для них
        // производительность в разрезе направлений неопределена.
        var hoursByDirection = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(li => li.PlannedHours));

        // Ключ — WorkDirectionId из снимка (UX_AnalysisVersionSnapshots_Version_WorkDirection),
        // а не из Norm: в справочнике может быть несколько норм с одним WorkDirectionId.
        var normByDirection = snapshots
            .Where(s => s.Norm is not null)
            .GroupBy(s => s.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First().Norm!);

        var rootDirections = workDirections.Where(d => !d.ParentId.HasValue).ToList();
        var result = new List<PerformanceRowDto>();
        var visitedDirections = new HashSet<int>();

        foreach (var root in rootDirections)
        {
            BuildHierarchy(root, 0, parentIdLookup, volumeByDirection, hoursByDirection, normByDirection, directionById, result, visitedDirections);
        }

        var unallocatedPhysicalVolume = physicalVolumes
            .Where(pv => pv.PhysicalVolume is not null && !pv.PhysicalVolume.ClassifierId.HasValue && pv.PhysicalVolume.IsAssessment == true)
            .Sum(pv => pv.QuantityInIicGroupUnits())
            + EquipmentVolumeAggregator.SumUnallocatedAssessmentVolume(equipmentRows);

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
            Interpretation: null,
            SubtreeHours: null));

        return result;
    }

    /// <summary>
    /// Рекурсивно строит иерархию направлений, добавляя в <paramref name="result"/> строки в pre-order.
    /// Возвращает агрегированные по поддереву (часы для оценки, сумма «оценка × часы»), используемые для
    /// средне-взвешенной фактической оценки родителей (вес = трудозатраты элемента).
    /// </summary>
    private static (decimal SubtreeEstimateHours, decimal SubtreeWeightedEstimate) BuildHierarchy(
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
        decimal subtreeEstimateHours = (ownFactEstimate.HasValue && factHours > 0) ? factHours : 0m;
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
            var (childEstimateHours, childWeighted) = BuildHierarchy(
                child,
                level + 1,
                parentIdLookup,
                volumeByDirection,
                hoursByDirection,
                normByDirection,
                directionById,
                result,
                visited);
            subtreeEstimateHours += childEstimateHours;
            subtreeWeightedEstimate += childWeighted;
        }

        // Агрегированная фактическая оценка по узлу: средне-взвешенное по часам.
        // Для листьев совпадает с собственной оценкой; для родителей — учитывает потомков.
        decimal? aggregatedFactEstimate = subtreeEstimateHours > 0
            ? subtreeWeightedEstimate / subtreeEstimateHours
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

        PerformanceInterpretation? interpretation = PerformanceInterpretationHelper.GetInterpretation(zScore);

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
            Interpretation: interpretation,
            SubtreeHours: subtreeEstimateHours > 0 ? subtreeEstimateHours : null);

        return (subtreeEstimateHours, subtreeWeightedEstimate);
    }

}
