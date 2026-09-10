using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}")]
[Authorize]
public sealed class ProjectLevelAnalyticsController : ControllerBase
{
    private readonly IProjectCompareService _compare;
    private readonly IGroupAnalyticsService _groupAnalytics;
    private readonly IVisualReportService _visualReports;
    private readonly IProjectExportService _projectExport;
    private readonly IDirectoryExportService _directoryExport;
    private readonly IExportFileNameService _fileNames;
    private readonly IProjectReportSummaryService _reportSummary;
    private readonly IProjectTrendService _projectTrend;
    private readonly ITrendExportService _trendExport;

    public ProjectLevelAnalyticsController(
        IProjectCompareService compare,
        IGroupAnalyticsService groupAnalytics,
        IVisualReportService visualReports,
        IProjectExportService projectExport,
        IDirectoryExportService directoryExport,
        IExportFileNameService fileNames,
        IProjectReportSummaryService reportSummary,
        IProjectTrendService projectTrend,
        ITrendExportService trendExport)
    {
        _compare = compare;
        _groupAnalytics = groupAnalytics;
        _visualReports = visualReports;
        _projectExport = projectExport;
        _directoryExport = directoryExport;
        _fileNames = fileNames;
        _reportSummary = reportSummary;
        _projectTrend = projectTrend;
        _trendExport = trendExport;
    }

    /// <summary>Сводка «план/факт» по основным версиям проекта для вкладки «Отчёт».</summary>
    [HttpGet("report-summary")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public Task<ProjectReportSummaryDto> ReportSummary(
        int projectId,
        CancellationToken cancellationToken = default) =>
        _reportSummary.GetSummaryAsync(projectId, cancellationToken);

    /// <summary>Вкладка «Тренд» отчёта проекта: Z по датам, мост часов, Z от 0, S-curve.</summary>
    [HttpGet("report-trend")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Reports, AccessLevel.Read)]
    public Task<ProjectTrendDto> ReportTrend(
        int projectId,
        CancellationToken cancellationToken = default) =>
        _projectTrend.GetAsync(projectId, cancellationToken);

    [HttpGet("report-trend/export.xlsx")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Reports, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportReportTrendExcel(int projectId, CancellationToken cancellationToken)
    {
        var bytes = await _trendExport.ExportProjectExcelAsync(projectId, cancellationToken);
        var fileName = await _fileNames.ForProjectAsync(
            projectId,
            ExportFileNameBuilder.Modules.Report,
            "xlsx",
            ExportFileNameBuilder.Views.Trend,
            cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("report-trend/export.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Reports, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportReportTrendPdf(int projectId, CancellationToken cancellationToken)
    {
        var bytes = await _trendExport.ExportProjectPdfAsync(projectId, cancellationToken);
        var fileName = await _fileNames.ForProjectAsync(
            projectId,
            ExportFileNameBuilder.Modules.Report,
            "pdf",
            ExportFileNameBuilder.Views.Trend,
            cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("compare")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public async Task<VersionCompareResultEncodedDto> Compare(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken = default)
    {
        var data = await _compare.CompareAsync(projectId, versionA, versionB, cancellationToken);
        return VersionCompareResultEncodedDto.WithoutDetail(data);
    }

    [HttpGet("compare/detail")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public async Task<EncodedGridDto> CompareDetail(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken = default)
    {
        var data = await _compare.CompareAsync(projectId, versionA, versionB, cancellationToken);
        return CompareDetailEncoder.Encode(data.Detail);
    }

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
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "pdf",
            ExportFileNameBuilder.CompareView(section),
            cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Excel текущего вида раздела сравнения (видимые колонки и отфильтрованные строки).</summary>
    [HttpPost("compare/export/{section}/excel-visible")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCompareExcelVisible(
        int projectId,
        string section,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        [FromBody] DirectoryExportRequest request,
        [FromQuery] bool visibleOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompareExportSection(section))
            return BadRequest("Поддерживаются разделы summary, facility и detail.");

        var bytes = _directoryExport.ExportToExcel(request);
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "xlsx",
            ExportFileNameBuilder.CompareView(section),
            visibleOnly ? ExportFileNameBuilder.Qualifiers.Visible : null,
            cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("compare/export/summary/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCompareSummaryExcel(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken)
    {
        var bytes = await _projectExport.ExportCompareSummaryExcelAsync(
            projectId, versionA, versionB, cancellationToken);
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "xlsx",
            ExportFileNameBuilder.Views.Summary,
            cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("compare/export/facility/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCompareFacilityExcel(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken)
    {
        var bytes = await _projectExport.ExportCompareFacilityExcelAsync(
            projectId, versionA, versionB, cancellationToken);
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "xlsx",
            ExportFileNameBuilder.Views.Facility,
            cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("compare/export/detail/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCompareDetailExcel(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken)
    {
        var bytes = await _projectExport.ExportCompareDetailExcelAsync(
            projectId, versionA, versionB, cancellationToken);
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "xlsx",
            ExportFileNameBuilder.Views.Detail,
            cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("compare/export/detail/summary-table")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.VersionComparison, AccessLevel.Read)]
    public async Task<IActionResult> ExportCompareDetailSummaryTable(
        int projectId,
        [FromQuery] int versionA,
        [FromQuery] int versionB,
        CancellationToken cancellationToken)
    {
        var bytes = await _projectExport.ExportCompareDetailSummaryTableAsync(
            projectId, versionA, versionB, cancellationToken);
        var fileName = await _fileNames.ForVersionsAsync(
            projectId,
            [versionA, versionB],
            ExportFileNameBuilder.Modules.Compare,
            "xlsx",
            ExportFileNameBuilder.Views.Detail,
            ExportFileNameBuilder.Qualifiers.SummaryTable,
            cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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

    private static bool IsCompareExportSection(string section) =>
        section.Equals("summary", StringComparison.OrdinalIgnoreCase)
        || section.Equals("facility", StringComparison.OrdinalIgnoreCase)
        || section.Equals("detail", StringComparison.OrdinalIgnoreCase);
}
