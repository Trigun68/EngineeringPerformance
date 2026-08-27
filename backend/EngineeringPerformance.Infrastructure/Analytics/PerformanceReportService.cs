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
    IPerformanceCalculationEngine calculationEngine,
    IProjectCharacteristicSelectionService characteristics,
    IProjectAnalyticsCache cache,
    ProjectsPageRawDataLoader rawLoader) : IPerformanceReportService
{
    public async Task<PerformanceReportDto> GetReportAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
        // v5: Z плана в таблице направлений = zBase параметров планирования; факт = PLI.PlannedHours.
        var cacheKey = $"report:v5:p{planVersionId?.ToString() ?? "auto"}:f{factVersionId?.ToString() ?? "auto"}";
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

        // Батч-предзагрузка bundle всех версий истории: без этого каждый блок
        // (план/факт/точки истории) грузил таблицы версии заново (N+1).
        var historyVersions = versions
            .Where(v => v.Direction is AnalysisDirection.Plan or AnalysisDirection.Fact)
            .ToList();
        var bundleByVersion = new Dictionary<int, ProjectsPageVersionBundle>(
            await rawLoader.LoadManyAsync(
                historyVersions.Select(v => (projectId, v.Id)).ToList(),
                cancellationToken));
        var snapshotKeyByVersion = await LoadSnapshotKeysAsync(
            versions.Select(v => v.Id).ToList(),
            cancellationToken);

        if (assessmentId is int assessId)
        {
            planBlock = await BuildPlanBlockAsync(
                projectId,
                assessId,
                bundleByVersion,
                cancellationToken);
        }

        if (factId is int fid)
        {
            factBlock = await BuildFactBlockAsync(projectId, fid, bundleByVersion, cancellationToken);
        }

        var history = new List<PerformanceReportVersionHistoryPointDto>();
        foreach (var version in historyVersions)
        {
            snapshotKeyByVersion.TryGetValue(version.Id, out var snapKey);
            history.Add(await BuildHistoryPointAsync(
                projectId,
                version,
                snapKey,
                bundleByVersion,
                cancellationToken));
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

    private async Task<ProjectsPageVersionBundle> GetBundleAsync(
        int projectId,
        int versionId,
        Dictionary<int, ProjectsPageVersionBundle> bundleCache,
        CancellationToken cancellationToken)
    {
        if (bundleCache.TryGetValue(versionId, out var cached))
            return cached;
        var bundle = await rawLoader.LoadAsync(projectId, versionId, cancellationToken);
        bundleCache[versionId] = bundle;
        return bundle;
    }

    private async Task<PerformanceReportVersionBlockDto> BuildPlanBlockAsync(
        int projectId,
        int versionId,
        Dictionary<int, ProjectsPageVersionBundle> bundleCache,
        CancellationToken cancellationToken)
    {
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

        var bundle = await GetBundleAsync(projectId, versionId, bundleCache, cancellationToken);
        var planPerformance = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, calculationEngine);
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, planPerformance, calculationEngine);
        var scenarios = PlanningScenarioReportMetrics.Compute(bundle, calculationEngine, forecast.GrandTotals);

        // Часы уже в bundle — повторный запрос LI не нужен.
        var labor = bundle.LaborIntensities;

        var directions = MapPlanDirectionRows(calculationEngine, bundle, planPerformance);
        var weightedZ = scenarios.ZBase;
        var totalHours = scenarios.HoursBase;
        var unallocated = FactLaborHoursTotals.UnallocatedPlannedHours(labor);
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(
            planPerformance,
            totalHours ?? 0m,
            unallocated,
            weightedZ);

        return ToVersionBlock(version, summary, totalHours, directions);
    }

    private async Task<PerformanceReportVersionBlockDto> BuildFactBlockAsync(
        int projectId,
        int versionId,
        Dictionary<int, ProjectsPageVersionBundle> bundleCache,
        CancellationToken cancellationToken)
    {
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

        // Производительность из bundle через движок — без повторной загрузки версии.
        var bundle = await GetBundleAsync(projectId, versionId, bundleCache, cancellationToken);
        var performance = ProjectsPageGridBuilder.BuildPerformance(bundle, calculationEngine);
        var labor = bundle.LaborIntensities;

        var totalHours = FactLaborHoursTotals.FromAllocatedPlannedHours(labor);
        var unallocated = FactLaborHoursTotals.UnallocatedPlannedHours(labor);
        var weightedZ = totalHours is > 0
            ? calculationEngine.ComputeWeightedZFromPerformanceRows(
                performance,
                decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces)
            : null;
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(
            performance,
            totalHours ?? 0m,
            unallocated,
            weightedZ);

        return ToVersionBlock(version, summary, totalHours, MapDirectionRows(performance));
    }

    private static PerformanceReportVersionBlockDto ToVersionBlock(
        Domain.Entities.ProjectAnalysisVersion version,
        ProjectVersionAnalysisSummaryDto summary,
        decimal? totalLaborHours,
        IReadOnlyList<PerformanceReportDirectionRowDto> directions) =>
        new(
            VersionId: version.Id,
            VersionName: version.VersionName,
            CreatedAt: version.CreatedAt,
            Direction: version.Direction,
            WeightedZScore: summary.WeightedZScore,
            WeightedZScorePercentile: summary.WeightedZScorePercentile,
            WeightedZScoreInterpretation: summary.WeightedZScoreInterpretation?.ToRussian(),
            TotalLaborHours: totalLaborHours,
            UnallocatedLaborHours: summary.UnallocatedLaborHours,
            PhysicalVolumeTotalsByUnit: summary.PhysicalVolumeTotalsByUnit,
            Directions: directions);

    /// <summary>
    /// Z плана в таблице направлений — заложенный zBase параметров планирования,
    /// не live Z от часов/нормы (тот остаётся в гриде прогноза).
    /// </summary>
    internal static List<PerformanceReportDirectionRowDto> MapPlanDirectionRows(
        IPerformanceCalculationEngine engine,
        ProjectsPageVersionBundle bundle,
        IReadOnlyList<PerformanceRowDto> performance) =>
        performance
            .Where(r => r.WorkDirectionId >= 0 && !r.IsTotal)
            .Select(r =>
            {
                var zBase = engine.ResolvePlanningZ(bundle.PlanningZByDirection, r.WorkDirectionId).ZBase;
                return new PerformanceReportDirectionRowDto(
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
                    ZScore: zBase,
                    Interpretation: PerformanceInterpretationHelper.GetInterpretation(zBase)?.ToRussian());
            })
            .ToList();

    private static List<PerformanceReportDirectionRowDto> MapDirectionRows(
        IReadOnlyList<PerformanceRowDto> performance) =>
        performance
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

    private async Task<Dictionary<int, string?>> LoadSnapshotKeysAsync(
        IReadOnlyList<int> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count == 0)
            return new Dictionary<int, string?>();

        var rows = await context.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => versionIds.Contains(s.ProjectAnalysisVersionId))
            .Select(s => new { s.ProjectAnalysisVersionId, s.WorkDirectionId, s.NormId })
            .ToListAsync(cancellationToken);

        return versionIds.ToDictionary(
            id => id,
            id => AnalysisVersionSnapshotService.FormatKey(
                rows.Where(r => r.ProjectAnalysisVersionId == id)
                    .Select(r => (r.WorkDirectionId, r.NormId))));
    }

    private async Task<PerformanceReportVersionHistoryPointDto> BuildHistoryPointAsync(
        int projectId,
        Domain.Entities.ProjectAnalysisVersion version,
        string? normSnapshotKey,
        Dictionary<int, ProjectsPageVersionBundle> bundleCache,
        CancellationToken cancellationToken)
    {
        var bundle = await GetBundleAsync(projectId, version.Id, bundleCache, cancellationToken);
        var planPerformance = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, calculationEngine);
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(
            bundle, planPerformance, calculationEngine);
        var scenarios = PlanningScenarioReportMetrics.Compute(bundle, calculationEngine, forecast.GrandTotals);

        decimal? plottedZ;
        decimal? plottedHours;
        if (version.Direction == AnalysisDirection.Fact)
        {
            var allocated = FactLaborHoursTotals.FromAllocatedPlannedHours(bundle.LaborIntensities);
            plottedHours = allocated;
            plottedZ = allocated is > 0
                ? calculationEngine.ComputeWeightedZFromPerformanceRows(
                    ProjectsPageGridBuilder.BuildPerformance(bundle, calculationEngine),
                    decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces)
                : null;
        }
        else
        {
            plottedZ = scenarios.ZBase;
            plottedHours = scenarios.HoursBase;
        }

        return ToHistoryPoint(version, plottedZ, plottedHours, normSnapshotKey, scenarios);
    }

    private static PerformanceReportVersionHistoryPointDto ToHistoryPoint(
        Domain.Entities.ProjectAnalysisVersion version,
        decimal? plottedZ,
        decimal? plottedHours,
        string? normSnapshotKey,
        PlanningScenarioReportMetrics.Result scenarios) =>
        new(
            VersionId: version.Id,
            VersionName: version.VersionName,
            CreatedAt: version.CreatedAt,
            Direction: version.Direction,
            WeightedZScore: plottedZ,
            TotalLaborHours: plottedHours,
            NormSnapshotKey: normSnapshotKey,
            OptimisticZScore: scenarios.ZOptimistic,
            OptimisticLaborHours: scenarios.HoursOptimistic,
            BaseZScore: scenarios.ZBase,
            BaseLaborHours: scenarios.HoursBase,
            PessimisticZScore: scenarios.ZPessimistic,
            PessimisticLaborHours: scenarios.HoursPessimistic);

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
