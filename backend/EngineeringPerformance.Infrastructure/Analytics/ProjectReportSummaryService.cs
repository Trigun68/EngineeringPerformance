using EngineeringPerformance.Application.Analytics;
<<<<<<< HEAD
using EngineeringPerformance.Domain.Entities;
=======
using EngineeringPerformance.Application.Services;
>>>>>>> origin/master
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
<<<<<<< HEAD
/// Сводка «план/факт» проекта: план берётся из основной версии Fact (её <c>PlannedHours</c>),
/// либо, если основной Fact-версии нет, из основной версии Plan; факт — из основной версии Fact
/// (её <c>ActualHours</c>), либо пуст, если основная версия Fact не назначена.
/// </summary>
public sealed class ProjectReportSummaryService : IProjectReportSummaryService
{
    private readonly AppDbContext _context;
    private readonly IPerformanceService _performanceService;

    public ProjectReportSummaryService(AppDbContext context, IPerformanceService performanceService)
    {
        _context = context;
        _performanceService = performanceService;
    }

    public async Task<ProjectReportSummaryDto> GetSummaryAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var mainVersions = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId
                        && v.IsMain
=======
/// Сводка «план/факт» проекта: план берётся из основной версии Fact (её сценарные BaseHours),
/// либо, если основной Fact-версии нет, из основной версии Plan; факт — из основной версии Fact
/// (allocated PlannedHours), либо пуст, если версий Fact нет.
/// Выбор версий и часы — та же семантика, что у <see cref="PerformanceReportService"/> (v3);
/// вся математика Z — только через <see cref="IPerformanceCalculationEngine"/>.
/// </summary>
public sealed class ProjectReportSummaryService(
    AppDbContext context,
    IPerformanceCalculationEngine calculationEngine,
    IProjectAnalyticsCache cache,
    ProjectsPageRawDataLoader rawLoader) : IProjectReportSummaryService
{
    private const string CacheKey = "report-summary:v1";

    public async Task<ProjectReportSummaryDto> GetSummaryAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<ProjectReportSummaryDto>(projectId, CacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var versions = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId
>>>>>>> origin/master
                        && !v.IsArchived
                        && (v.Direction == AnalysisDirection.Plan || v.Direction == AnalysisDirection.Fact))
            .ToListAsync(cancellationToken);

<<<<<<< HEAD
        var mainPlan = mainVersions.FirstOrDefault(v => v.Direction == AnalysisDirection.Plan);
        var mainFact = mainVersions.FirstOrDefault(v => v.Direction == AnalysisDirection.Fact);

        var planSourceVersion = mainFact ?? mainPlan;

        ProjectReportSummaryBlock? planBlock = null;
        if (planSourceVersion is not null)
            planBlock = await BuildBlockAsync(projectId, planSourceVersion, useActualHours: false, cancellationToken);

        ProjectReportSummaryBlock? factBlock = null;
        if (mainFact is not null)
            factBlock = await BuildBlockAsync(projectId, mainFact, useActualHours: true, cancellationToken);

        return new ProjectReportSummaryDto(
            PlanVersionId: planSourceVersion?.Id,
            PlanVersionName: planSourceVersion?.VersionName,
            PlanHours: planBlock?.Hours,
            PlanWeightedZScore: planBlock?.WeightedZScore,
            PlanPercentile: planBlock?.Percentile,
            PlanInterpretation: planBlock?.Interpretation,
            FactVersionId: mainFact?.Id,
            FactVersionName: mainFact?.VersionName,
            FactHours: factBlock?.Hours,
            FactWeightedZScore: factBlock?.WeightedZScore,
            FactPercentile: factBlock?.Percentile,
            FactInterpretation: factBlock?.Interpretation);
    }

    private async Task<ProjectReportSummaryBlock> BuildBlockAsync(
        int projectId,
        ProjectAnalysisVersion version,
        bool useActualHours,
        CancellationToken cancellationToken)
    {
        var laborIntensities = await _context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == version.Id)
            .ToListAsync(cancellationToken);

        var performanceRows = await _performanceService.GetPerformanceDataAsync(projectId, version.Id, useActualHours);
        var totalHours = FactLaborHoursTotals.FromLaborIntensities(laborIntensities, useActualHours);

        return ProjectReportSummaryCalculator.Calculate(performanceRows, totalHours);
    }
=======
        // План-метрики: primary Fact → иначе primary Plan (как в сводном отчёте производительности).
        var planSourceId = PerformanceReportService.ResolveAssessmentVersionId(versions, null, null);
        // Факт-метрики: только Fact-direction, без подстановки Plan.
        var factId = PerformanceReportService.ResolveVersionId(versions, AnalysisDirection.Fact, null);

        ProjectReportSummaryBlock? planBlock = null;
        string? planVersionName = null;
        if (planSourceId is int pid)
        {
            var bundle = await rawLoader.LoadAsync(projectId, pid, cancellationToken);
            planBlock = BuildPlanBlock(bundle);
            planVersionName = versions.First(v => v.Id == pid).VersionName;
        }

        ProjectReportSummaryBlock? factBlock = null;
        string? factVersionName = null;
        if (factId is int fid)
        {
            var bundle = await rawLoader.LoadAsync(projectId, fid, cancellationToken);
            factBlock = BuildFactBlock(bundle);
            factVersionName = versions.First(v => v.Id == fid).VersionName;
        }

        var dto = new ProjectReportSummaryDto(
            PlanVersionId: planSourceId,
            PlanVersionName: planVersionName,
            PlanHours: planBlock?.Hours,
            PlanWeightedZScore: planBlock?.WeightedZScore,
            PlanPercentile: planBlock?.Percentile,
            PlanInterpretation: planBlock?.Interpretation?.ToRussian(),
            FactVersionId: factId,
            FactVersionName: factVersionName,
            FactHours: factBlock?.Hours,
            FactWeightedZScore: factBlock?.WeightedZScore,
            FactPercentile: factBlock?.Percentile,
            FactInterpretation: factBlock?.Interpretation?.ToRussian());

        await cache.SetAsync(projectId, CacheKey, dto, cancellationToken);
        return dto;
    }

    /// <summary>План: производительность по сценарию версии; часы = BaseHours прогноза (GrandTotals).</summary>
    private ProjectReportSummaryBlock BuildPlanBlock(ProjectsPageVersionBundle bundle)
    {
        var planPerformance = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, calculationEngine);
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, planPerformance, calculationEngine);

        var hours = forecast.GrandTotals.BaseHours > 0 ? forecast.GrandTotals.BaseHours : (decimal?)null;
        var weightedZ = calculationEngine.ComputeWeightedZFromPerformanceRows(
            planPerformance,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);

        return ToBlock(hours, weightedZ);
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
>>>>>>> origin/master
}
