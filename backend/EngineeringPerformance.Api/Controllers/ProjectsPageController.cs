using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/versions/{versionId:int}")]
public sealed class ProjectsPageController : ControllerBase
{
    private readonly IProjectsPageDataFetcher _fetcher;
    private readonly IProjectExportService _exportService;
    private readonly IVisualReportService _visualReports;
    private readonly IDirectoryExportService _directoryExport;
    private readonly IExportFileNameService _fileNames;

    public ProjectsPageController(
        IProjectsPageDataFetcher fetcher,
        IProjectExportService exportService,
        IVisualReportService visualReports,
        IDirectoryExportService directoryExport,
        IExportFileNameService fileNames)
    {
        _fetcher = fetcher;
        _exportService = exportService;
        _visualReports = visualReports;
        _directoryExport = directoryExport;
        _fileNames = fileNames;
    }

    /// <summary>Batch-данные для гридов ProjectsPage без строк «Детально».</summary>
    /// <remarks>Гриды «Детально» — <see cref="GetPageDataDetails"/>; формат <see cref="EncodedGridDto"/>.</remarks>
    [HttpGet("page-data")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<ProjectsPageDataEncodedDto>> GetPageData(
        int projectId,
        int versionId,
        [FromQuery] int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var data = await _fetcher.GetEncodedPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);
        return Ok(data);
    }

    /// <summary>Упакованные гриды «Детально» (прогноз / факт / сравнение) из того же кэша, что page-data.</summary>
    [HttpGet("page-data/detail")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<ProjectsPageDetailsEncodedDto>> GetPageDataDetails(
        int projectId,
        int versionId,
        [FromQuery] int? compareVersionId,
        CancellationToken cancellationToken)
    {
        var data = await _fetcher.GetEncodedPageDetailsAsync(
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
        var fileName = await _fileNames.ForVersionAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Modules.Report,
            "pdf",
            ExportFileNameBuilder.Views.Version,
            cancellationToken: cancellationToken);
        return File(pdfBytes, "application/pdf", fileName);
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
        var fileName = await _fileNames.ForVersionAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Modules.Report,
            "xlsx",
            ExportFileNameBuilder.Views.Version,
            cancellationToken: cancellationToken);
        return File(
            excelBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
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
        var fileName = await ForecastFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Facility, "xlsx", cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("export/forecast-summary/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastSummaryExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportForecastSummaryExcelAsync(projectId, versionId, cancellationToken);
        var fileName = await ForecastFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Summary, "xlsx", cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("export/forecast-summary/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastSummaryPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportForecastSummaryPdfAsync(projectId, versionId, cancellationToken);
        var fileName = await ForecastFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Summary, "pdf", cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("export/forecast-facility/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastFacilityPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        // Визуальный движок (QuestPdfReportComposer), как у остальных PDF вкладки.
        var bytes = await _visualReports.ExportForecastFacilityPdfAsync(projectId, versionId, cancellationToken);
        var fileName = await ForecastFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Facility, "pdf", cancellationToken);
        return File(bytes, "application/pdf", fileName);
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
        var fileName = await ForecastFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Detail, "xlsx", cancellationToken);
        return File(bytes, ExcelContentType, fileName);
    }

    /// <summary>Excel текущего вида грида «Детально» (видимые колонки + отфильтрованные строки).</summary>
    [HttpPost("export/forecast-detail/excel-visible")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportForecastDetailExcelVisible(
        int projectId,
        int versionId,
        [FromBody] DirectoryExportRequest request,
        CancellationToken cancellationToken)
    {
        var bytes = _directoryExport.ExportToExcel(request);
        var fileName = await ForecastFileNameAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Views.Detail,
            "xlsx",
            cancellationToken,
            ExportFileNameBuilder.Qualifiers.Visible);
        return File(bytes, ExcelContentType, fileName);
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
        var fileName = await ForecastFileNameAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Views.Detail,
            "xlsx",
            cancellationToken,
            ExportFileNameBuilder.Qualifiers.SummaryTable);
        return File(bytes, ExcelContentType, fileName);
    }

    [HttpGet("export/fact-summary/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactSummaryExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportFactSummaryExcelAsync(projectId, versionId, cancellationToken);
        var fileName = await FactFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Summary, "xlsx", cancellationToken);
        return File(bytes, ExcelContentType, fileName);
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
        var fileName = await FactFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Summary, "pdf", cancellationToken);
        return File(bytes, "application/pdf", fileName);
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
        var fileName = await FactFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Facility, "pdf", cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("export/fact-facility/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactFacilityExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportFactFacilityExcelAsync(projectId, versionId, cancellationToken);
        var fileName = await FactFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Facility, "xlsx", cancellationToken);
        return File(bytes, ExcelContentType, fileName);
    }

<<<<<<< HEAD
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
=======
    [HttpGet("export/fact-detail/excel")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactDetailExcel(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
>>>>>>> origin/master
    {
        var bytes = await _exportService.ExportFactDetailExcelAsync(projectId, versionId, cancellationToken);
        var fileName = await FactFileNameAsync(projectId, versionId, ExportFileNameBuilder.Views.Detail, "xlsx", cancellationToken);
        return File(bytes, ExcelContentType, fileName);
    }

    /// <summary>Сводный PDF «план/факт» по основным версиям проекта (вкладка «Отчёт»).</summary>
    [HttpGet("export/report/pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportProjectReportPdf(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _visualReports.ExportProjectReportPdfAsync(projectId, versionId, cancellationToken);
        var fileName = await _fileNames.ForVersionAsync(
            projectId, versionId, ExportFileNameBuilder.Modules.Report, "pdf", cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Excel текущего вида грида факта «Детально» (видимые колонки + отфильтрованные строки).</summary>
    [HttpPost("export/fact-detail/excel-visible")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportFactDetailExcelVisible(
        int projectId,
        int versionId,
        [FromBody] DirectoryExportRequest request,
        CancellationToken cancellationToken)
    {
        var bytes = _directoryExport.ExportToExcel(request);
        var fileName = await FactFileNameAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Views.Detail,
            "xlsx",
            cancellationToken,
            ExportFileNameBuilder.Qualifiers.Visible);
        return File(bytes, ExcelContentType, fileName);
    }

    [HttpGet("export/fact-detail/summary-table")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportFactDetailSummaryTable(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bytes = await _exportService.ExportFactDetailSummaryTableAsync(projectId, versionId, cancellationToken);
        var fileName = await FactFileNameAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Views.Detail,
            "xlsx",
            cancellationToken,
            ExportFileNameBuilder.Qualifiers.SummaryTable);
        return File(bytes, ExcelContentType, fileName);
    }

    private Task<string> ForecastFileNameAsync(
        int projectId,
        int versionId,
        string view,
        string extension,
        CancellationToken cancellationToken,
        string? qualifier = null) =>
        _fileNames.ForVersionAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Modules.Forecast,
            extension,
            view,
            qualifier,
            cancellationToken);

    private Task<string> FactFileNameAsync(
        int projectId,
        int versionId,
        string view,
        string extension,
        CancellationToken cancellationToken,
        string? qualifier = null) =>
        _fileNames.ForVersionAsync(
            projectId,
            versionId,
            ExportFileNameBuilder.Modules.Fact,
            extension,
            view,
            qualifier,
            cancellationToken);

    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
