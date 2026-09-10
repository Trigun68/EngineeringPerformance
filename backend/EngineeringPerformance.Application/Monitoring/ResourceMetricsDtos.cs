namespace EngineeringPerformance.Application.Monitoring;

public sealed class ResourceMonitoringOptions
{
    public const string SectionName = "Monitoring";

    public int SampleIntervalSeconds { get; set; } = 10;

    public int RetentionMinutes { get; set; } = 60;

    public int ActiveUserWindowMinutes { get; set; } = 5;

    public double SqlMemoryLimitMib { get; set; } = 2048;

    public int HistoryRetentionDays { get; set; } = 90;
}

public sealed record ResourceMetricsDto(
    ResourceSnapshotDto Snapshot,
    ResourceSeriesDto Series,
    IReadOnlyList<string> Verdicts,
    DateTime CollectedAtUtc,
    int WindowMinutes,
    int BucketSeconds,
    DateTime? HistoryAvailableFromUtc);

public sealed record ResourceSnapshotDto(
    int ActiveUsers,
    int ActiveSessions,
    int KestrelConnections,
    int CurrentHttpRequests,
    SqlLaneConnectionsDto SqlConnections,
    int RedisClients,
    int AnalyticsGateBusy,
    int AnalyticsGateLimit,
    ServiceResourceDto Backend,
    ServiceResourceDto Redis,
    ServiceResourceDto Sql,
    double? CpuCoresPerUser,
    double? RssMibPerUser,
    BackendRuntimeDto Runtime);

public sealed record SqlLaneConnectionsDto(
    int Interactive,
    int Analytics,
    int Other,
    int Total);

public sealed record ServiceResourceDto(
    bool Available,
    string? Status,
    double CpuCores,
    double RssMib);

public sealed record BackendRuntimeDto(
    double GcHeapMib,
    double AllocRateBytesPerSecond,
    int Gen2Collections,
    int ThreadPoolBusy,
    long ThreadPoolQueue,
    int SqlRunnable,
    double RedisHitRatio,
    double RedisOpsPerSec,
    long RedisEvictedKeys,
    double RedisFragmentation,
    double SqlBatchRequestsPerSec,
    double SqlBufferCacheHitRatio);

public sealed record ResourceSeriesDto(
    IReadOnlyList<MetricsTimeSeriesPointDto> BackendCpuCores,
    IReadOnlyList<MetricsTimeSeriesPointDto> RedisCpuCores,
    IReadOnlyList<MetricsTimeSeriesPointDto> SqlCpuCores,
    IReadOnlyList<MetricsTimeSeriesPointDto> BackendRssMib,
    IReadOnlyList<MetricsTimeSeriesPointDto> RedisRssMib,
    IReadOnlyList<MetricsTimeSeriesPointDto> SqlRssMib,
    IReadOnlyList<MetricsTimeSeriesPointDto> ActiveUsers,
    IReadOnlyList<MetricsTimeSeriesPointDto> KestrelConnections,
    IReadOnlyList<MetricsTimeSeriesPointDto> SqlInteractive,
    IReadOnlyList<MetricsTimeSeriesPointDto> SqlAnalytics,
    IReadOnlyList<MetricsTimeSeriesPointDto> RedisClients,
    IReadOnlyList<MetricsTimeSeriesPointDto> ThreadPoolQueue,
    IReadOnlyList<MetricsTimeSeriesPointDto> SqlRunnable,
    IReadOnlyList<MetricsTimeSeriesPointDto> RedisHitRatio,
    IReadOnlyList<MetricsTimeSeriesPointDto> AllocRateMibPerSec,
    IReadOnlyList<MetricsTimeSeriesPointDto> Gen2Collections,
    double SqlMemoryLimitMib);

public interface IResourceMetricsService
{
    Task<ResourceMetricsDto> GetResourceMetricsAsync(int windowMinutes = 60, CancellationToken cancellationToken = default);
}

public interface IActiveUserTracker
{
    void Record(int userId, DateTime timestampUtc);

    int CountActive(TimeSpan window, DateTime nowUtc);
}

/// <summary>Снимок для авто-вердикта (без привязки к инфраструктуре).</summary>
public sealed record ResourceVerdictInput(
    DateTime TimestampUtc,
    int ActiveUsers,
    int SqlConnections,
    int AnalyticsGateBusy,
    int AnalyticsGateLimit,
    double BackendCpuCores,
    double RedisCpuCores,
    double SqlCpuCores,
    double BackendRssMib,
    double RedisRssMib,
    double SqlRssMib,
    double AllocRateBytesPerSecond,
    int Gen2Collections);
