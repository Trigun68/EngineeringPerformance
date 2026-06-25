namespace EngineeringPerformance.Application.Monitoring;

public sealed record SerilogLogEntryDto(
    string TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? SourceContext);

public sealed record LogViewerResultDto(
    IReadOnlyList<SerilogLogEntryDto> Entries,
    int TotalCount);

public interface ILogViewerService
{
    Task<LogViewerResultDto> GetRecentLogsAsync(
        int take = 1000,
        string? level = null,
        string? search = null,
        CancellationToken cancellationToken = default);
}

public sealed record ServiceHealthDto(
    string Name,
    bool IsConfigured,
    bool IsHealthy,
    string Status,
    double? ResponseTimeMs);

public sealed record HealthCheckResultDto(
    DateTime CheckedAtUtc,
    IReadOnlyList<ServiceHealthDto> Services,
    IReadOnlyList<MetricsTimeSeriesPointDto> ResponseTimeTrend24h);

public interface IHealthCheckService
{
    Task<HealthCheckResultDto> GetHealthAsync(CancellationToken cancellationToken = default);
}

public sealed record ConfigAuditEntryDto(
    int Id,
    int UserId,
    string UserName,
    DateTime TimestampUtc,
    string Category,
    string? EntityType,
    int? EntityId,
    string? Description);

public interface IAuditConfigService
{
    Task<IReadOnlyList<ConfigAuditEntryDto>> GetConfigAuditAsync(
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int? userId = null,
        string? search = null,
        int take = 500,
        CancellationToken cancellationToken = default);
}
