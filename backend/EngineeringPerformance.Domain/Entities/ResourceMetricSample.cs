namespace EngineeringPerformance.Domain.Entities;

/// <summary>Сэмпл потребления бэкенда / Redis / SQL для вкладки «Ресурсы».</summary>
public sealed class ResourceMetricSample
{
    public DateTime TimestampUtc { get; set; }

    public int ActiveUsers { get; set; }

    public int ActiveSessions { get; set; }

    public int KestrelConnections { get; set; }

    public int CurrentHttpRequests { get; set; }

    public int SqlInteractive { get; set; }

    public int SqlAnalytics { get; set; }

    public int SqlOther { get; set; }

    public int RedisClients { get; set; }

    public int AnalyticsGateBusy { get; set; }

    public int AnalyticsGateLimit { get; set; }

    public bool BackendAvailable { get; set; }

    public double BackendCpuCores { get; set; }

    public double BackendRssMib { get; set; }

    public bool RedisAvailable { get; set; }

    public string? RedisStatus { get; set; }

    public double RedisCpuCores { get; set; }

    public double RedisRssMib { get; set; }

    public bool SqlAvailable { get; set; }

    public string? SqlStatus { get; set; }

    public double SqlCpuCores { get; set; }

    public double SqlRssMib { get; set; }

    public double GcHeapMib { get; set; }

    public double AllocRateBytesPerSecond { get; set; }

    public int Gen2Collections { get; set; }

    public int ThreadPoolBusy { get; set; }

    public long ThreadPoolQueue { get; set; }

    public int SqlRunnable { get; set; }

    public double RedisHitRatio { get; set; }

    public double RedisOpsPerSec { get; set; }

    public long RedisEvictedKeys { get; set; }

    public double RedisFragmentation { get; set; }

    public double SqlBatchRequestsPerSec { get; set; }

    public double SqlBufferCacheHitRatio { get; set; }
}
