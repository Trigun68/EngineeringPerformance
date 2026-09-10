using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Сводка «план/факт» проекта: план берётся из основной версии Fact (её сценарные BaseHours),
/// либо, если основной Fact-версии нет, из основной версии Plan; факт — из основной версии Fact
/// (allocated PlannedHours), либо пуст, если версий Fact нет.
/// Выбор версий и часы — та же семантика, что у <see cref="PerformanceReportService"/> (v4);
/// Z плана — заложенный средневзвешенный Z параметров планирования (как в диалоге).
/// Вся математика Z — только через <see cref="IPerformanceCalculationEngine"/>.
/// </summary>
public sealed class ProjectReportSummaryService(
    AppDbContext context,
    IPerformanceCalculationEngine calculationEngine,
    IProjectAnalyticsCache cache,
    ProjectsPageRawDataLoader rawLoader) : IProjectReportSummaryService
{
    private const string CacheKey = "report-summary:v3";

    public async Task<ProjectReportSummaryDto> GetSummaryAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<ProjectReportSummaryDto>(projectId, CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var versions = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId
                        && !v.IsArchived
                        && (v.Direction == AnalysisDirection.Plan || v.Direction == AnalysisDirection.Fact))
            .ToListAsync(cancellationToken);

        // План-метрики: primary Fact → иначе primary Plan (как в сводном отчёте производительности).
        var planSourceId = PerformanceReportService.ResolveAssessmentVersionId(versions, null, null);
        // Факт-метрики: только Fact-direction, без подстановки Plan.
        var factId = PerformanceReportService.ResolveVersionId(versions, AnalysisDirection.Fact, null);

        ProjectReportSummaryBlock? planBlock = null;
        string? planVersionName = null;
        int? planSourceDirection = null;
        var planIsPrimary = false;
        if (planSourceId is int pid)
        {
            var bundle = await rawLoader.LoadAsync(projectId, pid, cancellationToken);
            planBlock = BuildPlanBlock(bundle);
            var planVersion = versions.First(v => v.Id == pid);
            planVersionName = planVersion.VersionName;
            planSourceDirection = (int)planVersion.Direction;
            planIsPrimary = planVersion.IsPrimary;
        }

        ProjectReportSummaryBlock? factBlock = null;
        string? factVersionName = null;
        var factIsPrimary = false;
        if (factId is int fid)
        {
            var bundle = await rawLoader.LoadAsync(projectId, fid, cancellationToken);
            factBlock = BuildFactBlock(bundle);
            var factVersion = versions.First(v => v.Id == fid);
            factVersionName = factVersion.VersionName;
            factIsPrimary = factVersion.IsPrimary;
        }

        var dto = new ProjectReportSummaryDto(
            PlanVersionId: planSourceId,
            PlanVersionName: planVersionName,
            PlanSourceDirection: planSourceDirection,
            PlanIsPrimary: planIsPrimary,
            PlanHours: planBlock?.Hours,
            PlanWeightedZScore: planBlock?.WeightedZScore,
            PlanPercentile: planBlock?.Percentile,
            PlanInterpretation: planBlock?.Interpretation?.ToRussian(),
            FactVersionId: factId,
            FactVersionName: factVersionName,
            FactIsPrimary: factIsPrimary,
            FactHours: factBlock?.Hours,
            FactWeightedZScore: factBlock?.WeightedZScore,
            FactPercentile: factBlock?.Percentile,
            FactInterpretation: factBlock?.Interpretation?.ToRussian());

        await cache.SetAsync(projectId, CacheKey, dto, cancellationToken);
        return dto;
    }

    /// <summary>
    /// План: часы = BaseHours прогноза (GrandTotals);
    /// Z = средневзвешенный заложенный zBase листьев (параметры планирования).
    /// </summary>
    private ProjectReportSummaryBlock BuildPlanBlock(ProjectsPageVersionBundle bundle)
    {
        var planPerformance = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, calculationEngine);
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, planPerformance);
        var scenarios = PlanningScenarioReportMetrics.Compute(bundle, calculationEngine, forecast.GrandTotals);

        return ToBlock(scenarios.HoursBase, scenarios.ZBase);
    }

    /// <summary>Факт: часы = allocated PlannedHours (ActualHours хранится, в Z/KPI не входит).</summary>
    private ProjectReportSummaryBlock BuildFactBlock(ProjectsPageVersionBundle bundle)
    {
        var performance = ProjectsPageGridBuilder.BuildPerformance(bundle, calculationEngine);
        var hours = FactLaborHoursTotals.FromAllocatedPlannedHours(bundle.LaborIntensities);
        var weightedZ = hours is > 0
            ? calculationEngine.ComputeWeightedZFromPerformanceRows(
                performance,
                decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces)
            : null;

        return ToBlock(hours, weightedZ);
    }

    private static ProjectReportSummaryBlock ToBlock(decimal? hours, decimal? weightedZ) =>
        new(
            Hours: hours,
            WeightedZScore: weightedZ,
            Percentile: PerformancePercentileHelper.GetPercentile(weightedZ),
            Interpretation: PerformanceInterpretationHelper.GetInterpretation(weightedZ));

    /// <summary>Один блок (план или факт) сводки «план/факт».</summary>
    private sealed record ProjectReportSummaryBlock(
        decimal? Hours,
        decimal? WeightedZScore,
        string Percentile,
        PerformanceInterpretation? Interpretation);
}
