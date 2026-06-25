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

    public ProjectsPageController(
        IProjectsPageDataFetcher fetcher,
        IProjectExportService exportService)
    {
        _fetcher = fetcher;
        _exportService = exportService;
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
}
