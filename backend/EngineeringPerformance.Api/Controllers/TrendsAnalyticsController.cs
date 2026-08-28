using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EngineeringPerformance.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/analytics/trends")]
[Authorize]
public sealed class TrendsAnalyticsController : ControllerBase
{
    private readonly ITrendAnalyticsService _trends;
    private readonly IZTrendAnalyticsService _zTrends;
    private readonly IProductivityTrendAnalyticsService _productivityTrends;
    private readonly IProductivityIndexTrendService _productivityIndexTrends;
    private readonly IVisualReportService _visualReports;
    private readonly IExportFileNameService _fileNames;
    private readonly ITrendExportService _trendExport;

    public TrendsAnalyticsController(
        ITrendAnalyticsService trends,
        IZTrendAnalyticsService zTrends,
        IProductivityTrendAnalyticsService productivityTrends,
        IProductivityIndexTrendService productivityIndexTrends,
        IVisualReportService visualReports,
        IExportFileNameService fileNames,
        ITrendExportService trendExport)
    {
        _trends = trends;
        _zTrends = zTrends;
        _productivityTrends = productivityTrends;
        _productivityIndexTrends = productivityIndexTrends;
        _visualReports = visualReports;
        _fileNames = fileNames;
        _trendExport = trendExport;
    }

    [HttpGet("formulas")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public ActionResult<IReadOnlyList<AssessmentFormulaDescriptor>> ListFormulas() =>
        Ok(_trends.ListFormulas());

    [HttpGet]
    [RequireProjectAccess]
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
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<IReadOnlyList<TrendDetailRowDto>> GetDetail(
        [FromQuery] int projectId,
        [FromQuery] int versionId,
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        CancellationToken cancellationToken = default) =>
        _trends.GetDetailAsync(projectId, versionId, formulaId, cancellationToken);

    /// <summary>Трендовый анализ средневзвешенного Z (план/факт, сегментация по нормам).</summary>
    [HttpPost("z-score")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<ActionResult<ZTrendResponse>> GetZTrends(
        [FromBody] ZTrendRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _zTrends.GetZTrendsAsync(request ?? new ZTrendRequest(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("z-score/export.xlsx")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportZTrendsExcel(
        [FromBody] ZTrendRequest? request,
        CancellationToken cancellationToken)
    {
        var bytes = await _trendExport.ExportAnalyticsExcelAsync(request ?? new ZTrendRequest(), cancellationToken);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Analytics,
            "xlsx",
            ExportFileNameBuilder.Views.Trend);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("z-score/export.pdf")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportZTrendsPdf(
        [FromBody] ZTrendRequest? request,
        CancellationToken cancellationToken)
    {
        var bytes = await _trendExport.ExportAnalyticsPdfAsync(request ?? new ZTrendRequest(), cancellationToken);
        var fileName = ExportFileNameBuilder.Build(
            ExportFileNameBuilder.Modules.Analytics,
            "pdf",
            ExportFileNameBuilder.Views.Trend);
        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet("z-score/filters")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public Task<ZTrendFiltersDto> GetZTrendFilters(CancellationToken cancellationToken) =>
        _zTrends.GetFiltersAsync(cancellationToken);

    /// <summary>
    /// Тренд производительности нормы: часы N(V*) через ComputeForecastHours, без сравнения коэффициентов a/b.
    /// </summary>
    [HttpPost("productivity")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<ActionResult<ProductivityTrendResponse>> GetProductivityTrend(
        [FromBody] ProductivityTrendRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _productivityTrends.GetProductivityTrendAsync(
            request ?? new ProductivityTrendRequest(),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Тренд индекса производительности (% к норме) по основным версиям: Σ(V×N)/Σ(часы)×100
    /// через PerformanceCalculationEngine.ComputeProductivityIndex.
    /// </summary>
    [HttpPost("productivity-index")]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    public async Task<ActionResult<ProductivityIndexTrendResponse>> GetProductivityIndexTrend(
        [FromBody] ProductivityIndexTrendRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _productivityIndexTrends.GetProductivityIndexTrendAsync(
            request ?? new ProductivityIndexTrendRequest(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("export.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.GroupTrend, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportTrendsPdf(
        [FromQuery] int projectId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        CancellationToken cancellationToken = default)
    {
        var bytes = await _visualReports.ExportTrendsPdfAsync(projectId, dateFrom, dateTo, formulaId, cancellationToken);
        var fileName = await _fileNames.ForProjectAsync(
            projectId,
            ExportFileNameBuilder.Modules.Analytics,
            "pdf",
            ExportFileNameBuilder.Views.Trends,
            cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }
}
