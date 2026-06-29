using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public MonitoringController(
        ISystemMetricsService systemMetrics,
        ILogViewerService logViewer,
        IHealthCheckService healthCheck,
        IAuditConfigService auditConfig,
        IPermissionDriftService permissionDrift)
    {
        _systemMetrics = systemMetrics;
        _logViewer = logViewer;
        _healthCheck = healthCheck;
        _auditConfig = auditConfig;
        _permissionDrift = permissionDrift;
    }

    [HttpGet("system-metrics")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics(
        [FromQuery] int windowMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        var metrics = await _systemMetrics.GetSystemMetricsAsync(windowMinutes, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("logs")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<LogViewerResultDto>> GetLogs(
        [FromQuery] int take = 1000,
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

    [HttpGet("audit-config")]
    [HasPermission(PermissionType.Administration, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ConfigAuditEntryDto>>> GetAuditConfig(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? userId = null,
        [FromQuery] string? search = null,
        [FromQuery] int take = 500,
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
}
