using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/versions/{versionId:int}")]
public sealed class ProjectsPageController : ControllerBase
{
    private readonly IProjectsPageDataFetcher _fetcher;
    private readonly IProjectExportService _exportService;
    private readonly IVisualReportService _visualReports;
    private readonly IForecastDetailSummaryTableContext _summaryTableContext;

    public ProjectsPageController(
        IProjectsPageDataFetcher fetcher,
        IProjectExportService exportService,
        IVisualReportService visualReports,
        IForecastDetailSummaryTableContext summaryTableContext)
    {
        _fetcher = fetcher;
        _exportService = exportService;
        _visualReports = visualReports;
        _summaryTableContext = summaryTableContext;
    }

    /// <summary>Batch-данные для 9 гридов ProjectsPage (прогноз / факт / сравнение).</summary>
    [HttpGet("page-data")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<ProjectsPageDataDto>> GetPageData(
        int projectId,
        int versionId,
        [FromQuery] int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var data = await _fetcher.GetProjectsPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);
        return Ok(data);
    }

    [HttpGet("export/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportPdf(
        int projectId,
        int versionId,
        [FromQuery] int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var pdfBytes = await _exportService.ExportToPdfAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);
        return File(pdfBytes, "application/pdf", $"Project_{projectId}_v{versionId}.pdf");
    }

    [HttpGet("export/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportExcel(
        int projectId,
        int versionId,
        [FromQuery] int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var excelBytes = await _exportService.ExportToExcelAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);
        return File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Project_{projectId}_v{versionId}.xlsx");
    }

    [HttpGet("export/forecast-facility/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastFacilityExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportForecastFacilityExcelAsync(projectId, versionId, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ForecastFacility_{projectId}_v{versionId}.xlsx");
    }

    [HttpGet("export/forecast-summary/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastSummaryPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportForecastSummaryPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"ForecastSummary_{projectId}_v{versionId}.pdf");
    }

    [HttpGet("export/forecast-facility/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastFacilityPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportForecastFacilityPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"ForecastFacility_{projectId}_v{versionId}.pdf");
    }

    [HttpGet("export/forecast-detail/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastDetailExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportForecastDetailExcelAsync(projectId, versionId, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var safeProject = SanitizeFileName(projectCode);
        var safeVersion = SanitizeFileName(versionName);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{safeProject}_{safeVersion}_Детально_{timestamp}.xlsx");
    }

    [HttpGet("export/forecast-detail/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastDetailPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportForecastDetailPdfAsync(projectId, versionId, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var safeProject = SanitizeFileName(projectCode);
        var safeVersion = SanitizeFileName(versionName);
        return File(bytes, "application/pdf", $"{safeProject}_{safeVersion}_Детально_{timestamp}.pdf");
    }

    [HttpGet("export/forecast-detail/summary-table")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastDetailSummaryTable(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportForecastDetailSummaryTableAsync(projectId, versionId, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ForecastDetailSummary_{projectId}_v{versionId}.xlsx");
    }

    [HttpGet("export/fact-summary/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactSummaryPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportFactSummaryPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"FactSummary_{projectId}_v{versionId}.pdf");
    }

    [HttpGet("export/fact-facility/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactFacilityPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportFactFacilityPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"FactFacility_{projectId}_v{versionId}.pdf");
    }

    [HttpGet("export/fact-detail/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactDetailPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportFactDetailPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"FactDetail_{projectId}_v{versionId}.pdf");
    }

    /// <summary>PDF вкладки «Отчёт»: сводка план/факт по основным версиям проекта + топ-5/детали фактической версии.</summary>
    [HttpGet("export/report/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportProjectReportPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportProjectReportPdfAsync(projectId, versionId, cancellationToken);
        var (projectCode, versionName, _) = await _summaryTableContext.LoadAsync(projectId, versionId, cancellationToken);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var safeProject = SanitizeFileName(projectCode);
        var safeVersion = SanitizeFileName(versionName);
        return File(bytes, "application/pdf", $"{safeProject}_{safeVersion}_Отчёт_{timestamp}.pdf");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Проект" : cleaned;
    }
}
