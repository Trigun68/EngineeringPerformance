using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed record ResourceSample(
    DateTime TimestampUtc,
    int ActiveUsers,
    int ActiveSessions,
    int KestrelConnections,
    int CurrentHttpRequests,
    int SqlInteractive,
    int SqlAnalytics,
    int SqlOther,
    int RedisClients,
    int AnalyticsGateBusy,
    int AnalyticsGateLimit,
    bool BackendAvailable,
    double BackendCpuCores,
    double BackendRssMib,
    bool RedisAvailable,
    string? RedisStatus,
    double RedisCpuCores,
    double RedisRssMib,
    bool SqlAvailable,
    string? SqlStatus,
    double SqlCpuCores,
    double SqlRssMib,
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
    double SqlBufferCacheHitRatio)
{
    public int SqlConnectionsTotal => SqlInteractive + SqlAnalytics + SqlOther;

    public ResourceVerdictInput ToVerdictInput()
        => new(
            TimestampUtc,
            ActiveUsers,
            SqlConnectionsTotal,
            AnalyticsGateBusy,
            AnalyticsGateLimit,
            BackendCpuCores,
            RedisCpuCores,
            SqlCpuCores,
            BackendRssMib,
            RedisRssMib,
            SqlRssMib,
            AllocRateBytesPerSecond,
            Gen2Collections);
}
