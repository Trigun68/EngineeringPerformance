using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    private readonly ISystemMetricsService _systemMetrics;
    private readonly ILogViewerService _logViewer;
    private readonly IHealthCheckService _healthCheck;
    private readonly IAuditConfigService _auditConfig;
    private readonly IPermissionDriftService _permissionDrift;
    private readonly ISqlProfilingReportService _sqlProfiling;
    private readonly IResourceMetricsService _resourceMetrics;

    public MonitoringController(
        ISystemMetricsService systemMetrics,
        ILogViewerService logViewer,
        IHealthCheckService healthCheck,
        IAuditConfigService auditConfig,
        IPermissionDriftService permissionDrift,
        ISqlProfilingReportService sqlProfiling,
        IResourceMetricsService resourceMetrics)
    {
        _systemMetrics = systemMetrics;
        _logViewer = logViewer;
        _healthCheck = healthCheck;
        _auditConfig = auditConfig;
        _permissionDrift = permissionDrift;
        _sqlProfiling = sqlProfiling;
        _resourceMetrics = resourceMetrics;
    }

    [HttpGet("system-metrics")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics(
        [FromQuery][RangeAttribute(1, 10080)] int windowMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _systemMetrics.GetSystemMetricsAsync(windowMinutes, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("logs")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<LogViewerResultDto>> GetLogs(
        [FromQuery][RangeAttribute(1, 5000)] int take = 1000,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await _logViewer.GetRecentLogsAsync(take, level, search, cancellationToken);
        return Ok(logs);
    }

    [HttpGet("health")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<HealthCheckResultDto>> GetHealth(CancellationToken cancellationToken = default)
    {
        var health = await _healthCheck.GetHealthAsync(cancellationToken);
        return Ok(health);
    }

    [HttpGet("resource-metrics")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<ResourceMetricsDto>> GetResourceMetrics(
        [FromQuery][RangeAttribute(1, 129600)] int windowMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _resourceMetrics.GetResourceMetricsAsync(windowMinutes, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("audit-config")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ConfigAuditEntryDto>>> GetAuditConfig(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? userId = null,
        [FromQuery] string? search = null,
        [FromQuery][RangeAttribute(1, 2000)] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var entries = await _auditConfig.GetConfigAuditAsync(from, to, userId, search, take, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("permission-drift")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<PermissionDriftDto>> GetPermissionDrift(CancellationToken cancellationToken = default)
    {
        var drift = await _permissionDrift.GetPermissionDriftAsync(cancellationToken);
        return Ok(drift);
    }

    /// <summary>Сводка SQL-профилирования: slow / frequent / weak tables / size correlation.</summary>
    [HttpGet("sql-metrics/summary")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<SqlProfilingSummaryDto>> GetSqlMetricsSummary(
        [FromQuery][RangeAttribute(1, 168)] int hours = 24,
        [FromQuery][RangeAttribute(1, 500)] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var summary = await _sqlProfiling.GetSummaryAsync(hours, take, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("sql-metrics/slow")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<SqlSlowQueryDto>>> GetSlowSql(
        [FromQuery][RangeAttribute(1, 168)] int hours = 24,
        [FromQuery][RangeAttribute(1, 500)] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _sqlProfiling.GetSlowQueriesAsync(hours, take, cancellationToken));

    [HttpGet("sql-metrics/frequent")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<SqlFrequentQueryDto>>> GetFrequentSql(
        [FromQuery][RangeAttribute(1, 168)] int hours = 24,
        [FromQuery][RangeAttribute(1, 500)] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _sqlProfiling.GetFrequentQueriesAsync(hours, take, cancellationToken));

    [HttpGet("sql-metrics/weak-tables")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<SqlWeakTableDto>>> GetWeakTables(
        [FromQuery][RangeAttribute(1, 168)] int hours = 24,
        [FromQuery][RangeAttribute(1, 500)] int take = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _sqlProfiling.GetWeakTablesAsync(hours, take, cancellationToken));
}
