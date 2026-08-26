using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
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
                        && !v.IsArchived
                        && (v.Direction == AnalysisDirection.Plan || v.Direction == AnalysisDirection.Fact))
            .ToListAsync(cancellationToken);

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
}
