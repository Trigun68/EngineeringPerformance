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
[Route("api/projects/{projectId:int}/performance-report")]
public sealed class PerformanceReportController(
    IPerformanceReportService reportService,
    IVisualReportService visualReports,
    IExportFileNameService fileNames) : ControllerBase
{
    [HttpGet]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Reports, AccessLevel.Read)]
    public async Task<ActionResult<PerformanceReportDto>> Get(
        int projectId,
        [FromQuery] int? planVersionId = null,
        [FromQuery] int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var report = await reportService.GetReportAsync(
            projectId,
            planVersionId,
            factVersionId,
            cancellationToken);
        return Ok(report);
    }

    [HttpGet("export.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Reports, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportPdf(
        int projectId,
        [FromQuery] int? planVersionId = null,
        [FromQuery] int? factVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = await visualReports.ExportPerformanceReportPdfAsync(
            projectId,
            planVersionId,
            factVersionId,
            cancellationToken);
        var fileName = await fileNames.ForPerformanceReportAsync(
            projectId,
            planVersionId,
            factVersionId,
            cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }
}
