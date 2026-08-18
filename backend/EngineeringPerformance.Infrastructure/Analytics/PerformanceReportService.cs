using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Агрегат отчёта производительности: план + факт + история версий + insights.
/// Математика Z — только через <see cref="IPerformanceCalculationEngine"/>.
/// </summary>
public sealed class PerformanceReportService(
    AppDbContext context,
    IPerformanceService performanceService,
    IPerformanceCalculationEngine calculationEngine,
    IProjectCharacteristicSelectionService characteristics,
    IProjectAnalyticsCache cache) : IPerformanceReportService
{
    public async Task<PerformanceReportDto> GetReportAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
        // Отчёт считает план + факт + историю всех версий — самый дорогой read-путь (ISS-018).
        var cacheKey = $"report:p{planVersionId?.ToString() ?? "auto"}:f{factVersionId?.ToString() ?? "auto"}";
        var cached = await cache.GetAsync<PerformanceReportDto>(projectId, cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var project = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var versions = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived)
            .OrderBy(v => v.CreatedAt)
            .ThenBy(v => v.Direction)
            .ThenBy(v => v.Id)
            .ToListAsync(cancellationToken);

        var planCount = versions.Count(v => v.Direction == AnalysisDirection.Plan);
        var factCount = versions.Count(v => v.Direction == AnalysisDirection.Fact);

        // Plan-метрики: primary Fact → иначе primary Plan (query id — только для совместимости PDF/тестов).
        var assessmentId = ResolveAssessmentVersionId(versions, planVersionId, factVersionId);
        // Fact-метрики: только Fact-direction (primary → latest); без подстановки Plan.
        var factId = ResolveVersionId(versions, AnalysisDirection.Fact, factVersionId);

        PerformanceReportVersionBlockDto? planBlock = null;
        PerformanceReportVersionBlockDto? factBlock = null;

        if (assessmentId is int assessId)
        {
            planBlock = await BuildVersionBlockAsync(
                projectId,
                assessId,
                LaborHoursKind.Planned,
                applyPlanningZLabor: true,
                cancellationToken);
        }

        if (factId is int fid)
        {
            factBlock = await BuildVersionBlockAsync(
                projectId,
                fid,
                LaborHoursKind.Actual,
                applyPlanningZLabor: false,
                cancellationToken);
        }

        var history = new List<PerformanceReportVersionHistoryPointDto>();
        foreach (var version in versions.Where(v =>
                     v.Direction is AnalysisDirection.Plan or AnalysisDirection.Fact))
        {
            history.Add(await BuildHistoryPointAsync(projectId, version, cancellationToken));
        }

        history = history
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Direction == AnalysisDirection.Plan ? 0 : 1)
            .ThenBy(h => h.VersionId)
            .ToList();

        var accuracy = await BuildAccuracyAsync(projectId, cancellationToken);
        var merged = MergeDirections(planBlock?.Directions, factBlock?.Directions);
        var insights = PerformanceReportInsightsBuilder.Build(merged);
        var normSources = CountNormSources(planBlock, factBlock);

        var report = new PerformanceReportDto(
            Project: new PerformanceReportProjectDto(
                Id: project.Id,
                Code: project.Code,
                Name: project.Name,
                Gip: project.Gip,
                Customer: project.Customer,
                WorkStatus: project.WorkStatus,
                Type: project.Type,
                PlanVersionCount: planCount,
                FactVersionCount: factCount),
            Plan: planBlock,
            Fact: factBlock,
            VersionHistory: history,
            Accuracy: accuracy,
            Insights: insights,
            Directions: merged,
            NormSourcesUsed: normSources);

        await cache.SetAsync(projectId, cacheKey, report, cancellationToken);
        return report;
    }

    /// <summary>
    /// Данные плана: primary Fact → иначе primary Plan → иначе latest Fact → иначе latest Plan.
    /// Явные query id (если валидны) имеют приоритет: factVersionId, затем planVersionId.
    /// </summary>
    public static int? ResolveAssessmentVersionId(
        IReadOnlyList<Domain.Entities.ProjectAnalysisVersion> versions,
        int? planVersionId,
        int? factVersionId)
    {
        if (factVersionId is int fid)
        {
            var match = versions.FirstOrDefault(v =>
                v.Id == fid && v.Direction == AnalysisDirection.Fact);
            if (match is not null)
                return match.Id;
        }

        if (planVersionId is int pid)
        {
            var match = versions.FirstOrDefault(v =>
                v.Id == pid && v.Direction == AnalysisDirection.Plan);
            if (match is not null)
                return match.Id;
        }

        var facts = versions.Where(v => v.Direction == AnalysisDirection.Fact).ToList();
        var plans = versions.Where(v => v.Direction == AnalysisDirection.Plan).ToList();

        var primaryFact = facts.FirstOrDefault(v => v.IsPrimary);
        if (primaryFact is not null)
            return primaryFact.Id;

        var primaryPlan = plans.FirstOrDefault(v => v.IsPrimary);
        if (primaryPlan is not null)
            return primaryPlan.Id;

        if (facts.Count > 0)
            return facts.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First().Id;

        if (plans.Count > 0)
            return plans.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First().Id;

        return null;
    }

    /// <summary>Явный id → IsPrimary → max CreatedAt внутри direction.</summary>
    public static int? ResolveVersionId(
        IReadOnlyList<Domain.Entities.ProjectAnalysisVersion> versions,
        AnalysisDirection direction,
        int? requestedId)
    {
        var ofDirection = versions.Where(v => v.Direction == direction).ToList();
        if (ofDirection.Count == 0)
            return null;

        if (requestedId is int id)
        {
            var match = ofDirection.FirstOrDefault(v => v.Id == id);
            if (match is not null)
                return match.Id;
        }

        var primary = ofDirection.FirstOrDefault(v => v.IsPrimary);
        if (primary is not null)
            return primary.Id;

        return ofDirection.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).First().Id;
    }

    private static int CountNormSources(
        PerformanceReportVersionBlockDto? plan,
        PerformanceReportVersionBlockDto? fact)
    {
        var count = 0;
        if (plan?.Directions.Any(d => d.NormProductivity.HasValue) == true)
            count++;
        if (fact?.Directions.Any(d => d.NormProductivity.HasValue) == true)
            count++;
        return count;
    }

    private async Task<PerformanceReportVersionBlockDto> BuildVersionBlockAsync(
        int projectId,
        int versionId,
        LaborHoursKind hoursKind,
        bool applyPlanningZLabor,
        CancellationToken cancellationToken)
    {
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

        var performance = await performanceService.GetPerformanceDataAsync(projectId, versionId, hoursKind);
        var labor = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var totalHours = hoursKind == LaborHoursKind.Actual
            ? FactLaborHoursTotals.FromActualHours(labor)
            : FactLaborHoursTotals.FromLaborIntensities(labor);
        var unallocated = hoursKind == LaborHoursKind.Actual
            ? FactLaborHoursTotals.UnallocatedActualHours(labor)
            : FactLaborHoursTotals.UnallocatedPlannedHours(labor);

        var weightedZ = calculationEngine.ComputeWeightedZFromPerformanceRows(
            performance,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(
            performance,
            totalHours ?? 0m,
            unallocated,
            weightedZ);

        var directions = performance
            .Where(r => r.WorkDirectionId >= 0 && !r.IsTotal)
            .Select(r => new PerformanceReportDirectionRowDto(
                WorkDirectionId: r.WorkDirectionId,
                ParentId: r.ParentId,
                Level: r.Level,
                Code: r.WorkDirectionCode,
                Name: r.WorkDirectionName,
                UnitName: r.NormUnitName,
                FactPhysicalVolume: r.FactPhysicalVolume,
                FactLaborHours: r.FactLaborHours,
                SubtreeHours: r.SubtreeHours,
                FactProductivity: r.FactProductivity,
                NormProductivity: r.NormProductivity,
                NormStdDev: r.NormStdDev,
                ZScore: r.ZScore,
                Interpretation: r.Interpretation?.ToRussian()))
            .ToList();

        if (applyPlanningZLabor)
            directions = await ApplyPlanningZLaborAsync(versionId, directions, cancellationToken);

        var totalLabor = applyPlanningZLabor
            ? SumDirectionLaborHours(directions) ?? summary.TotalFactLaborHours
            : summary.TotalFactLaborHours;

        return new PerformanceReportVersionBlockDto(
            VersionId: version.Id,
            VersionName: version.VersionName,
            CreatedAt: version.CreatedAt,
            Direction: version.Direction,
            WeightedZScore: summary.WeightedZScore,
            WeightedZScorePercentile: summary.WeightedZScorePercentile,
            WeightedZScoreInterpretation: summary.WeightedZScoreInterpretation?.ToRussian(),
            TotalLaborHours: totalLabor,
            UnallocatedLaborHours: summary.UnallocatedLaborHours,
            PhysicalVolumeTotalsByUnit: summary.PhysicalVolumeTotalsByUnit,
            Directions: directions);
    }

    /// <summary>
    /// Подставляет трудоёмкость плана с учётом ZBase (ForecastCalculation через engine).
    /// Если расчёт недоступен — оставляем PlannedHours из LI.
    /// </summary>
    private async Task<List<PerformanceReportDirectionRowDto>> ApplyPlanningZLaborAsync(
        int versionId,
        List<PerformanceReportDirectionRowDto> directions,
        CancellationToken cancellationToken)
    {
        var planningParams = await context.ProjectVersionPlanningParameters
            .AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var zByDirection = planningParams.ToDictionary(
            p => p.IicWorkDirectionId,
            p => new PlanningZParameters(p.ZPessimistic, p.ZBase, p.ZOptimistic));

        return directions.Select(d =>
        {
            if (d.FactPhysicalVolume is not > 0 || d.NormProductivity is null)
                return d;

            var z = calculationEngine.ResolvePlanningZ(zByDirection, d.WorkDirectionId);
            var (baseH, _, _) = calculationEngine.ComputeForecastHours(
                d.FactPhysicalVolume.Value,
                d.NormProductivity,
                d.NormStdDev,
                z);

            if (baseH is not > 0)
                return d;

            var hours = Math.Round(baseH.Value, 2);
            decimal? productivity = d.FactPhysicalVolume > 0
                ? Math.Round(hours / d.FactPhysicalVolume.Value, 4)
                : d.FactProductivity;

            return d with
            {
                FactLaborHours = hours,
                FactProductivity = productivity,
            };
        }).ToList();
    }

    private static decimal? SumDirectionLaborHours(IReadOnlyList<PerformanceReportDirectionRowDto> directions)
    {
        // Сумма только листьев (нет детей в наборе), чтобы не удвоить иерархию.
        var parentIds = directions
            .Where(d => d.ParentId.HasValue)
            .Select(d => d.ParentId!.Value)
            .ToHashSet();
        var leaves = directions.Where(d => !parentIds.Contains(d.WorkDirectionId)).ToList();
        var sum = leaves.Sum(d => d.FactLaborHours ?? 0m);
        return sum > 0 ? Math.Round(sum, 2) : null;
    }

    private async Task<PerformanceReportVersionHistoryPointDto> BuildHistoryPointAsync(
        int projectId,
        Domain.Entities.ProjectAnalysisVersion version,
        CancellationToken cancellationToken)
    {
        // Динамика Z — assessment path (PlannedHours), не ActualHours.
        var performance = await performanceService.GetPerformanceDataAsync(
            projectId, version.Id, LaborHoursKind.Planned);
        var labor = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == version.Id)
            .ToListAsync(cancellationToken);

        var weightedZ = calculationEngine.ComputeWeightedZFromPerformanceRows(
            performance,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);

        return new PerformanceReportVersionHistoryPointDto(
            VersionId: version.Id,
            VersionName: version.VersionName,
            CreatedAt: version.CreatedAt,
            Direction: version.Direction,
            WeightedZScore: weightedZ,
            TotalLaborHours: FactLaborHoursTotals.FromLaborIntensities(labor));
    }

    private async Task<PerformanceReportAccuracyDto> BuildAccuracyAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var selections = await characteristics.GetByProjectAsync(projectId, cancellationToken);
        var items = selections
            .Select(s => new PerformanceReportAccuracyItemDto(
                CharacteristicName: s.CharacteristicName,
                OptionName: s.SelectedOptionName,
                ValuePercent: s.SelectedValuePercent))
            .ToList();

        var percents = items
            .Select(i => i.ValuePercent)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        decimal? average = percents.Count > 0
            ? Math.Round(percents.Average(), 1)
            : null;

        return new PerformanceReportAccuracyDto(average, items);
    }

    public static IReadOnlyList<PerformanceReportMergedDirectionRowDto> MergeDirections(
        IReadOnlyList<PerformanceReportDirectionRowDto>? planRows,
        IReadOnlyList<PerformanceReportDirectionRowDto>? factRows)
    {
        var planById = (planRows ?? []).ToDictionary(r => r.WorkDirectionId);
        var factById = (factRows ?? []).ToDictionary(r => r.WorkDirectionId);
        var ids = planById.Keys.Union(factById.Keys).ToHashSet();

        var merged = new List<PerformanceReportMergedDirectionRowDto>();
        foreach (var id in ids)
        {
            planById.TryGetValue(id, out var plan);
            factById.TryGetValue(id, out var fact);
            var meta = fact ?? plan!;
            var volume = fact?.FactPhysicalVolume ?? plan?.FactPhysicalVolume;
            var norm = fact?.NormProductivity ?? plan?.NormProductivity;
            var sigma = fact?.NormStdDev ?? plan?.NormStdDev;
            var unit = fact?.UnitName ?? plan?.UnitName;

            merged.Add(new PerformanceReportMergedDirectionRowDto(
                WorkDirectionId: id,
                ParentId: meta.ParentId,
                Level: meta.Level,
                Code: meta.Code,
                Name: meta.Name,
                UnitName: unit,
                PhysicalVolume: volume,
                PlanLaborHours: plan?.FactLaborHours,
                FactLaborHours: fact?.FactLaborHours,
                PlanSubtreeHours: plan?.SubtreeHours,
                FactSubtreeHours: fact?.SubtreeHours,
                PlanProductivity: plan?.FactProductivity,
                FactProductivity: fact?.FactProductivity,
                NormProductivity: norm,
                NormStdDev: sigma,
                PlanZScore: plan?.ZScore,
                FactZScore: fact?.ZScore,
                PlanInterpretation: plan?.Interpretation,
                FactInterpretation: fact?.Interpretation));
        }

        return merged
            .OrderBy(r => r.Level)
            .ThenBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
