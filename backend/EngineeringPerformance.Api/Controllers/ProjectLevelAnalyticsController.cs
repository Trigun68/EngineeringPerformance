using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}")]
[Authorize]
public sealed class ProjectLevelAnalyticsController : ControllerBase
{
    private readonly IProjectCompareService _compare;
    private readonly IGroupAnalyticsService _groupAnalytics;
    private readonly IVisualReportService _visualReports;
    private readonly IProjectReportSummaryService _reportSummary;

    public ProjectLevelAnalyticsController(
        IProjectCompareService compare,
        IGroupAnalyticsService groupAnalytics,
        IVisualReportService visualReports,
        IProjectReportSummaryService reportSummary)
    {
        _compare = compare;
        _groupAnalytics = groupAnalytics;
        _visualReports = visualReports;
        _reportSummary = reportSummary;
    }

    [HttpGet("report-summary")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public Task<ProjectReportSummaryDto> ReportSummary(
        int projectId,
        CancellationToken cancellationToken = default) =>
        _reportSummary.GetSummaryAsync(projectId, cancellationToken);

    [HttpGet("compare")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public Task<VersionCompareResultDto> Compare(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken = default) =>
        _compare.CompareAsync(projectId, versionA, versionB, cancellationToken);

    [HttpGet("compare/chart")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public Task<CompareChartResultDto> CompareChart(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        [FromQuery] string level = "iic",
        [FromQuery] string metric = "zScore",
        [FromQuery] int top = 20,
        CancellationToken cancellationToken = default) =>
        _compare.GetChartAsync(projectId, versionA, versionB, level, metric, top, cancellationToken);

    [HttpGet("compare/export.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public async Task<IActionResult> ExportComparePdf(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        [FromQuery] string section = "full",
        [FromQuery] bool showOnlyDifferences = false,
        [FromQuery] int top = 20,
        CancellationToken cancellationToken = default)
    {
        var sectionEnum = section.ToLowerInvariant() switch
        {
            "summary" => CompareReportSection.Summary,
            "facility" => CompareReportSection.Facility,
            "detail" => CompareReportSection.Detail,
            _ => CompareReportSection.Full,
        };
        var bytes = await _visualReports.ExportComparePdfAsync(
            projectId, versionA, versionB, sectionEnum, showOnlyDifferences, top, cancellationToken);
        return File(bytes, "application/pdf", $"Compare_{projectId}_{versionA}_{versionB}.pdf");
    }

    [HttpGet("group-analysis")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupAnalysis, AccessLevel.Read)]
    public Task<GroupAnalysisResultDto> GroupAnalysis(
        int projectId,
        CancellationToken cancellationToken = default) =>
        _groupAnalytics.GetGroupAnalysisAsync(projectId, cancellationToken);

    [HttpGet("group-trend")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<GroupTrendInfoDto> GroupTrend(
        int projectId,
        CancellationToken cancellationToken = default) =>
        _groupAnalytics.GetGroupTrendAsync(projectId, cancellationToken);
}
