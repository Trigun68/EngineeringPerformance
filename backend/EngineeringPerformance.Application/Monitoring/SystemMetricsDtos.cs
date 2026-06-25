namespace EngineeringPerformance.Application.Monitoring;

public sealed record SystemMetricsDto(
    SystemMetricsRedDto Red,
    IReadOnlyList<MetricsTimeSeriesPointDto> RequestRateSeries,
    IReadOnlyList<ErrorDistributionItemDto> ErrorDistribution,
    IReadOnlyList<MetricsTimeSeriesPointDto> ResponseTimeTrend24h,
    DependencyHealthDto Database,
    DependencyHealthDto Redis,
    DateTime CollectedAtUtc);

public sealed record SystemMetricsRedDto(
    int TotalRequests,
    double AverageDurationMs,
    int ServerErrorCount,
    int WindowMinutes);

public sealed record MetricsTimeSeriesPointDto(string TimestampUtc, double Value);

public sealed record ErrorDistributionItemDto(string ErrorType, int Count);

public sealed record DependencyHealthDto(bool IsConfigured, bool IsConnected, string Status);

public interface ISystemMetricsService
{
    Task<SystemMetricsDto> GetSystemMetricsAsync(int windowMinutes = 15, CancellationToken cancellationToken = default);
}

public interface IRequestMetricsStore
{
    void Record(DateTime timestampUtc, double durationMs, int statusCode, string? exceptionType);

    SystemMetricsSnapshot GetSnapshot(int rateWindowMinutes);
}

public sealed record SystemMetricsSnapshot(
    SystemMetricsRedDto Red,
    IReadOnlyList<MetricsTimeSeriesPointDto> RequestRateSeries,
    IReadOnlyList<ErrorDistributionItemDto> ErrorDistribution,
    IReadOnlyList<MetricsTimeSeriesPointDto> ResponseTimeTrend24h);
