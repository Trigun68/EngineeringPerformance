using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/versions/{versionId:int}")]
[Authorize]
public sealed class ProjectAnalyticsController : ControllerBase
{
    private readonly IPerformanceService _performance;
    private readonly IProjectVersionAnalysisService _analysis;
    private readonly IProjectForecastService _forecast;
    private readonly IForecastPlannedVolumePersistenceService _plannedVolumePersistence;
    private readonly IAnalyticsExportService _export;

    public ProjectAnalyticsController(
        IPerformanceService performance,
        IProjectVersionAnalysisService analysis,
        IProjectForecastService forecast,
        IForecastPlannedVolumePersistenceService plannedVolumePersistence,
        IAnalyticsExportService export)
    {
        _performance = performance;
        _analysis = analysis;
        _forecast = forecast;
        _plannedVolumePersistence = plannedVolumePersistence;
        _export = export;
    }

    [HttpGet("performance")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public Task<IReadOnlyList<PerformanceRowDto>> Performance(
        int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _performance.GetPerformanceDataAsync(projectId, versionId);

    [HttpGet("analysis")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public Task<ProjectVersionAnalysisDto> Analysis(
        int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _analysis.GenerateReportAsync(projectId, versionId);

    [HttpGet("forecast")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public Task<ProjectForecastReportDto> Forecast(
        int projectId,
        int versionId,
        [FromQuery] string? method,
        [FromQuery] decimal? zOptimistic,
        [FromQuery] decimal? zBase,
        [FromQuery] decimal? zPessimistic,
        CancellationToken cancellationToken = default) =>
        _forecast.GenerateForecastAsync(
            projectId,
            versionId,
            calculation: BuildCalculationOptions(method, zOptimistic, zBase, zPessimistic));

    [HttpPost("forecast/preview")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public Task<ProjectForecastReportDto> ForecastPreview(
        int projectId,
        int versionId,
        [FromBody] ForecastPreviewRequestDto? request,
        CancellationToken cancellationToken = default) =>
        _forecast.GenerateForecastAsync(
            projectId,
            versionId,
            request?.NodePlannedTotals,
            request?.Calculation);

    [HttpPost("forecast/calculate")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public Task<ProjectForecastReportDto> ForecastCalculate(
        int projectId,
        int versionId,
        [FromBody] ForecastPreviewRequestDto? request,
        CancellationToken cancellationToken = default) =>
        _forecast.GenerateForecastAsync(
            projectId,
            versionId,
            request?.NodePlannedTotals,
            request?.Calculation);

    [HttpGet("forecast/assessment-volumes")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public Task<ProjectForecastAssessmentVolumesDto> ForecastAssessmentVolumes(
        int projectId, int versionId, CancellationToken cancellationToken = default) =>
        _forecast.GetAssessmentVolumesByDirectionAsync(projectId, versionId);

    [HttpPut("forecast/planned-volumes")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [HasPermission(PermissionType.Planning, AccessLevel.Edit)]
    public async Task<IActionResult> SaveForecastPlannedVolumes(
        int projectId,
        int versionId,
        [FromBody] SaveForecastPlannedVolumesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in request.Items)
        {
            await _plannedVolumePersistence.SavePlannedVolumeAsync(
                projectId,
                versionId,
                item.WorkDirectionId,
                item.Quantity,
                cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("export/analysis.xlsx")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportAnalysisExcel(
        int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var bytes = await _export.ExportAnalysisExcelAsync(projectId, versionId, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"analysis-{projectId}-{versionId}.xlsx");
    }

    [HttpGet("export/analysis.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Assessment, AccessLevel.Read)]
    public async Task<IActionResult> ExportAnalysisPdf(
        int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var bytes = await _export.ExportAnalysisPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"analysis-{projectId}-{versionId}.pdf");
    }

    [HttpGet("export/forecast.xlsx")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastExcel(
        int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var bytes = await _export.ExportForecastExcelAsync(projectId, versionId, cancellationToken);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"forecast-{projectId}-{versionId}.xlsx");
    }

    [HttpGet("export/forecast.pdf")]
    [RequireProjectAccess]
    [HasPermission(PermissionType.Planning, AccessLevel.Read)]
    public async Task<IActionResult> ExportForecastPdf(
        int projectId, int versionId, CancellationToken cancellationToken = default)
    {
        var bytes = await _export.ExportForecastPdfAsync(projectId, versionId, cancellationToken);
        return File(bytes, "application/pdf", $"forecast-{projectId}-{versionId}.pdf");
    }

    private static ForecastCalculationOptionsDto BuildCalculationOptions(
        string? method,
        decimal? zOptimistic,
        decimal? zBase,
        decimal? zPessimistic) =>
        new(
            Method: string.IsNullOrWhiteSpace(method) ? "regression" : method.Trim(),
            ZOptimistic: zOptimistic ?? -0.5m,
            ZBase: zBase ?? 0m,
            ZPessimistic: zPessimistic ?? 0.5m);
}
