using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сервис отчёта «Анализ версии проекта». Объединяет:
/// 1) полностью раскрытую иерархию направлений IIC (с ед. изм.),
/// 2) показатели производительности из <see cref="IPerformanceService"/>,
/// 3) ненулевые физ. объёмы проекта по каждому направлению (номенклатура + ед. изм. + кол-во),
/// 4) отдельный блок видов работ из трудоёмкости проекта.
/// </summary>
public sealed class ProjectVersionAnalysisService : IProjectVersionAnalysisService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPerformanceService _performanceService;
    private readonly IPerformanceCalculationEngine _calculationEngine;

    public ProjectVersionAnalysisService(
        IDbContextFactory<AppDbContext> contextFactory,
        IPerformanceService performanceService,
        IPerformanceCalculationEngine calculationEngine)
    {
        _contextFactory = contextFactory;
        _performanceService = performanceService;
        _calculationEngine = calculationEngine;
    }

    public async Task<ProjectVersionAnalysisDto> GenerateReportAsync(int projectId, int versionId)
    {
        await using var ctx1 = await _contextFactory.CreateDbContextAsync();
        await using var ctx2 = await _contextFactory.CreateDbContextAsync();
        await using var ctx3 = await _contextFactory.CreateDbContextAsync();
        await using var ctx4 = await _contextFactory.CreateDbContextAsync();

        var versionTask = ctx1.ProjectAnalysisVersions
            .AsNoTracking()
            .Include(v => v.Project)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId);

        var pvTask = ctx2.ProjectPhysicalVolumes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.PhysicalVolumeKfo).ThenInclude(k => k!.Unit)
            .Include(x => x.PhysicalVolumeBer).ThenInclude(b => b!.Unit)
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var eqTask = ctx4.ProjectEquipments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(e => e.Equipment).ThenInclude(e => e!.Unit)
            .Where(e => e.ProjectId == projectId && e.ProjectAnalysisVersionId == versionId)
            .ToListAsync();

        var laborTask = ctx3.ProjectLaborIntensities
            .AsNoTracking()
            .Include(li => li.WorkDirection)
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .OrderBy(li => li.DocumentationCode)
            .ToListAsync();

        var performanceTask = _performanceService.GetPerformanceDataAsync(projectId, versionId);

        await Task.WhenAll(versionTask, pvTask, eqTask, laborTask, performanceTask);

        var version = versionTask.Result
            ?? throw new InvalidOperationException($"Version {versionId} not found for project {projectId}");
        var projectPhysicalVolumes = pvTask.Result;
        var projectEquipments = eqTask.Result;
        var laborIntensities = laborTask.Result;
        var performanceData = ProjectsPageGridBuilder.AttachStoredPlanHours(
            performanceTask.Result,
            projectPhysicalVolumes);

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
                PhysicalVolumes: BuildPhysicalVolumes(pvByDirection, r.WorkDirectionId),
                EquipmentVolumes: BuildEquipmentVolumes(eqByDirection, r.WorkDirectionId),
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
                        : li.WorkDirection?.Name,
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

        var planningRows = await ctx1.ProjectVersionPlanningParameters
            .AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .Select(p => new { p.IicWorkDirectionId, p.ZPessimistic, p.ZBase, p.ZOptimistic })
            .ToListAsync();

        var planningZByDirection = planningRows.ToDictionary(
            p => p.IicWorkDirectionId,
            p => new PlanningZParameters(p.ZPessimistic, p.ZBase, p.ZOptimistic));

        return new ProjectVersionAnalysisDto(
            ProjectName: version.Project.Name,
            ProjectCode: version.Project.Code,
            VersionName: version.VersionName,
            Directions: directions,
            LaborIntensities: laborRows,
            Summary: summary,
            PlanningZByDirection: planningZByDirection);
    }

    private static IReadOnlyList<ProjectVersionPhysicalVolumeDto> BuildPhysicalVolumes(
        IReadOnlyDictionary<int, List<ProjectPhysicalVolume>> pvByDirection,
        int directionId)
    {
        if (!pvByDirection.TryGetValue(directionId, out var list))
            return Array.Empty<ProjectVersionPhysicalVolumeDto>();

        return list
            .Select(pv =>
            {
                var coeff = pv.GetCoefficient();
                return new ProjectVersionPhysicalVolumeDto(
                    Code: pv.GetVolumeCode(),
                    Name: pv.GetVolumeName(),
                    UnitName: IicWorkDirectionDisplayUnit.ResolveProjectPhysicalVolumeUnitName(pv),
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
        int directionId)
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
                    UnitName: eq.Equipment?.Unit?.Name,
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
                PlannedHours: li.PlannedHours))
            .ToList();
    }
}
