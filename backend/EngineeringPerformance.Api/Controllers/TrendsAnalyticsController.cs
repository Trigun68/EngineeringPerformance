using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EngineeringPerformance.Api.Authorization;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/analytics/trends")]
[Authorize]
public sealed class TrendsAnalyticsController : ControllerBase
{
    private readonly ITrendAnalyticsService _trends;
    private readonly IVisualReportService _visualReports;

    public TrendsAnalyticsController(ITrendAnalyticsService trends, IVisualReportService visualReports)
    {
        _trends = trends;
        _visualReports = visualReports;
    }

    [HttpGet("formulas")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public ActionResult<IReadOnlyList<AssessmentFormulaDescriptor>> ListFormulas() =>
        Ok(_trends.ListFormulas());

    [HttpGet]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<TrendAnalyticsResultDto> GetTrends(
        [FromQuery] int projectId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string granularity = "version",
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        CancellationToken cancellationToken = default) =>
        _trends.GetTrendsAsync(projectId, dateFrom, dateTo, granularity, formulaId, cancellationToken);

    [HttpGet("detail")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<IReadOnlyList<TrendDetailRowDto>> GetDetail(
        [FromQuery] int projectId,
        [FromQuery] int versionId,
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        CancellationToken cancellationToken = default) =>
        _trends.GetDetailAsync(projectId, versionId, formulaId, cancellationToken);

    [HttpGet("export.pdf")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<IActionResult> ExportTrendsPdf(
        [FromQuery] int projectId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _visualReports.ExportTrendsPdfAsync(projectId, dateFrom, dateTo, formulaId, cancellationToken);
        return File(bytes, "application/pdf", $"Trends_{projectId}.pdf");
    }
}
