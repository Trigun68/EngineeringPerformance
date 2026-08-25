using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сервис отчёта «Анализ версии проекта». Объединяет:
/// 1) полностью раскрытую иерархию направлений IIC (с ед. изм.),
/// 2) показатели производительности из движка <see cref="IPerformanceCalculationEngine"/> по bundle,
/// 3) ненулевые физ. объёмы проекта по каждому направлению (номенклатура + ед. изм. + кол-во),
/// 4) отдельный блок видов работ из трудоёмкости проекта.
/// </summary>
public sealed class ProjectVersionAnalysisService : IProjectVersionAnalysisService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPerformanceCalculationEngine _calculationEngine;
    private readonly ProjectsPageRawDataLoader _rawLoader;

    public ProjectVersionAnalysisService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPerformanceCalculationEngine calculationEngine,
        ProjectsPageRawDataLoader rawLoader)
    {
        _contextFactory = contextFactory;
        _calculationEngine = calculationEngine;
        _rawLoader = rawLoader;
    }

    public async Task<ProjectVersionAnalysisDto> GenerateReportAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        // Один проход загрузки: bundle читает таблицы версии один раз (analytics-lane).
        // Раньше здесь было 4 параллельных DbContext + повторное чтение тех же таблиц
        // в IPerformanceService — данные версии загружались трижды.
        var bundle = await _rawLoader.LoadAsync(projectId, versionId, cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Include(v => v.Project)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");

        // Ед. изм. отдельным словарём: bundle не тащит Unit-навигации каталогов (горячий путь page-data).
        var unitNameById = await context.UnitsOfMeasurement
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        var projectPhysicalVolumes = bundle.PhysicalVolumes;
        var projectEquipments = bundle.Equipment;
        var laborIntensities = bundle.LaborIntensities
            .OrderBy(li => li.DocumentationCode, StringComparer.Ordinal)
            .ToList();
        var performanceData = ProjectsPageGridBuilder.AttachLivePlanHours(
            ProjectsPageGridBuilder.BuildPerformance(bundle, _calculationEngine),
            bundle);

        // Группировка ненулевых физ. объёмов по направлению (ClassifierId).
        var pvByDirection = projectPhysicalVolumes
            .Where(pv => pv.GetClassifierId().HasValue == true && pv.Quantity > 0m && pv.GetIsAssessment() == true)
            .GroupBy(pv => pv.GetClassifierId()!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var eqByDirection = projectEquipments
            .Where(eq => eq.Equipment?.ClassifierId.HasValue == true && eq.Quantity > 0m && eq.Equipment.IsAssessment == true)
            .GroupBy(eq => eq.Equipment!.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Группировка трудоёмкости по направлению (WorkDirectionId).
        var laborByDirection = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Иерархия направлений: используем результат PerformanceService как готовую упорядоченную структуру.
        var directions = performanceData
            .Where(r => r.WorkDirectionId >= 0)
            .Select(r => new ProjectVersionDirectionDto(
                Id: r.WorkDirectionId,
                ParentId: r.ParentId,
                Level: r.Level,
                Code: r.WorkDirectionCode,
                Name: r.WorkDirectionName,
                UnitName: r.NormUnitName,
                FactPhysicalVolume: r.FactPhysicalVolume,
                FactLaborHours: r.FactLaborHours,
                PessimisticHours: r.PessimisticHours,
                BaseHours: r.BaseHours,
                OptimisticHours: r.OptimisticHours,
                FactProductivity: r.FactProductivity,
                FactEstimate: r.FactEstimate,
                NormProductivity: r.NormProductivity,
                NormEstimate: r.NormEstimate,
                NormStdDev: r.NormStdDev,
                ZScore: r.ZScore,
                Interpretation: r.Interpretation,
                PhysicalVolumes: BuildPhysicalVolumes(pvByDirection, r.WorkDirectionId, unitNameById),
                EquipmentVolumes: BuildEquipmentVolumes(eqByDirection, r.WorkDirectionId, unitNameById),
                DirectionLaborIntensities: BuildDirectionLabor(laborByDirection, r.WorkDirectionId)))
            .ToList();

        // Имена групп IIC уже в формате «код — наименование (ед.)» из PerformanceService.
        var directionNameById = performanceData
            .Where(r => r.WorkDirectionId >= 0)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First().WorkDirectionName);

        // Виды работ: общий блок из трудоёмкости проекта.
        var laborRows = laborIntensities
            .Select(li => new ProjectVersionLaborIntensityDto(
                DocumentationCode: li.DocumentationCode,
                Brand: li.Brand,
                WorkDirectionName: li.WorkDirectionId is int dirId
                    && directionNameById.TryGetValue(dirId, out var formattedName)
                        ? formattedName
                        : li.WorkDirectionId is int rawDirId
                            && bundle.ClassifierMap.TryGetValue(rawDirId, out var direction)
                                ? direction.Name
                                : null,
                PlannedHours: li.PlannedHours,
                ActualHours: li.ActualHours))
            .ToList();

        var totalPlannedHours = FactLaborHoursTotals.FromLaborIntensities(laborIntensities) ?? 0m;
        var unallocatedPlannedHours = FactLaborHoursTotals.UnallocatedPlannedHours(laborIntensities);

        var weightedZ = _calculationEngine.ComputeWeightedZFromPerformanceRows(
            performanceData,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(
            performanceData,
            totalPlannedHours,
            unallocatedPlannedHours,
            weightedZ);

        return new ProjectVersionAnalysisDto(
            ProjectName: version.Project!.Name,
            ProjectCode: version.Project.Code,
            VersionName: version.VersionName,
            Directions: directions,
            LaborIntensities: laborRows,
            Summary: summary,
            PlanningZByDirection: bundle.PlanningZByDirection);
    }

    private static IReadOnlyList<ProjectVersionPhysicalVolumeDto> BuildPhysicalVolumes(
        IReadOnlyDictionary<int, List<ProjectPhysicalVolume>> pvByDirection,
        int directionId,
        IReadOnlyDictionary<int, string> unitNameById)
    {
        if (!pvByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionPhysicalVolumeDto>();

        return list
            .Select(pv =>
            {
                var coeff = pv.GetCoefficient();
                var unitId = pv.PhysicalVolumeKfo?.UnitId ?? pv.PhysicalVolumeBer?.UnitId;
                return new ProjectVersionPhysicalVolumeDto(
                    Code: pv.GetVolumeCode(),
                    Name: pv.GetVolumeName(),
                    UnitName: unitId is int id && unitNameById.TryGetValue(id, out var unitName) ? unitName : null,
                    Quantity: pv.Quantity,
                    Coefficient: coeff,
                    Total: pv.Quantity * coeff);
            })
            .OrderBy(pv => pv.Code, StringComparer.Ordinal)
            .ThenBy(pv => pv.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<ProjectVersionPhysicalVolumeDto> BuildEquipmentVolumes(
        IReadOnlyDictionary<int, List<ProjectEquipment>> eqByDirection,
        int directionId,
        IReadOnlyDictionary<int, string> unitNameById)
    {
        if (!eqByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionPhysicalVolumeDto>();

        return list
            .Select(eq =>
            {
                var coeff = eq.Equipment?.Coefficient ?? 1m;
                return new ProjectVersionPhysicalVolumeDto(
                    Code: eq.CatalogId ?? string.Empty,
                    Name: eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                    UnitName: eq.Equipment?.UnitId is int uid && unitNameById.TryGetValue(uid, out var unitName)
                        ? unitName
                        : null,
                    Quantity: eq.Quantity,
                    Coefficient: coeff,
                    Total: eq.QuantityInIicGroupUnits());
            })
            .OrderBy(eq => eq.Code, StringComparer.Ordinal)
            .ThenBy(eq => eq.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<ProjectVersionDirectionLaborDto> BuildDirectionLabor(
        IReadOnlyDictionary<int, List<ProjectLaborIntensity>> laborByDirection,
        int directionId)
    {
        if (!laborByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionDirectionLaborDto>();

        return list
            .Select(li => new ProjectVersionDirectionLaborDto(
                DocumentationCode: li.DocumentationCode,
                PlannedHours: li.PlannedHours,
                ActualHours: li.ActualHours,
                Brand: li.Brand))
            .ToList();
    }
}
