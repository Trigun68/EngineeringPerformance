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
    IProjectCharacteristicSelectionService characteristics) : IPerformanceReportService
{
    public async Task<PerformanceReportDto> GetReportAsync(
        int projectId,
        int? planVersionId = null,
        int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
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

        var resolvedPlanId = ResolveVersionId(versions, AnalysisDirection.Plan, planVersionId);
        var resolvedFactId = ResolveVersionId(versions, AnalysisDirection.Fact, factVersionId);

        PerformanceReportVersionBlockDto? planBlock = null;
        PerformanceReportVersionBlockDto? factBlock = null;

        if (resolvedPlanId is int planId)
        {
            planBlock = await BuildVersionBlockAsync(projectId, planId, cancellationToken);
        }

        if (resolvedFactId is int factId)
        {
            factBlock = await BuildVersionBlockAsync(projectId, factId, cancellationToken);
        }

        var history = new List<PerformanceReportVersionHistoryPointDto>();
        foreach (var version in versions.Where(v =>
                     v.Direction is AnalysisDirection.Plan or AnalysisDirection.Fact))
        {
            var point = await BuildHistoryPointAsync(projectId, version, cancellationToken);
            history.Add(point);
        }

        history = history
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Direction == AnalysisDirection.Plan ? 0 : 1)
            .ThenBy(h => h.VersionId)
            .ToList();

        var accuracy = await BuildAccuracyAsync(projectId, cancellationToken);

        var insightSource = factBlock?.Directions ?? planBlock?.Directions
            ?? Array.Empty<PerformanceReportDirectionRowDto>();
        var insights = PerformanceReportInsightsBuilder.Build(
            insightSource,
            factBlock?.TotalLaborHours ?? planBlock?.TotalLaborHours,
            factBlock?.UnallocatedLaborHours ?? planBlock?.UnallocatedLaborHours,
            accuracy.AveragePercent);

        return new PerformanceReportDto(
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
            Insights: insights);
    }

    private static int? ResolveVersionId(
        IReadOnlyList<Domain.Entities.ProjectAnalysisVersion> versions,
        AnalysisDirection direction,
        int? requestedId)
    {
        if (requestedId is int id)
        {
            var match = versions.FirstOrDefault(v => v.Id == id && v.Direction == direction);
            if (match is not null) return match.Id;
        }

        return versions.FirstOrDefault(v => v.Direction == direction && v.IsPrimary)?.Id
            ?? versions.FirstOrDefault(v => v.Direction == direction)?.Id;
    }

    private async Task<PerformanceReportVersionBlockDto> BuildVersionBlockAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

        var performance = await performanceService.GetPerformanceDataAsync(projectId, versionId);
        var labor = await context.ProjectLaborIntensities
            .AsNoTracking()
            .Where(li => li.ProjectId == projectId && li.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        var totalHours = FactLaborHoursTotals.FromLaborIntensities(labor);
        var unallocated = FactLaborHoursTotals.UnallocatedPlannedHours(labor);
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

        return new PerformanceReportVersionBlockDto(
            VersionId: version.Id,
            VersionName: version.VersionName,
            CreatedAt: version.CreatedAt,
            Direction: version.Direction,
            WeightedZScore: summary.WeightedZScore,
            WeightedZScorePercentile: summary.WeightedZScorePercentile,
            WeightedZScoreInterpretation: summary.WeightedZScoreInterpretation?.ToRussian(),
            TotalLaborHours: summary.TotalFactLaborHours,
            UnallocatedLaborHours: summary.UnallocatedLaborHours,
            PhysicalVolumeTotalsByUnit: summary.PhysicalVolumeTotalsByUnit,
            Directions: directions);
    }

    private async Task<PerformanceReportVersionHistoryPointDto> BuildHistoryPointAsync(
        int projectId,
        Domain.Entities.ProjectAnalysisVersion version,
        CancellationToken cancellationToken)
    {
        var performance = await performanceService.GetPerformanceDataAsync(projectId, version.Id);
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
}
