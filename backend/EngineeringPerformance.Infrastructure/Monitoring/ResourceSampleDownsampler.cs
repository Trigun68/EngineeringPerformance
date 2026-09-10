using EngineeringPerformance.Application.Monitoring;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public static class ResourceSampleDownsampler
{
    private static readonly DateTime EpochUtc = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime BucketStartUtc(DateTime timestampUtc, int bucketSeconds)
    {
        var seconds = Math.Max(1, bucketSeconds);
        var offset = (timestampUtc.ToUniversalTime() - EpochUtc).TotalSeconds;
        var start = Math.Floor(offset / seconds) * seconds;
        return EpochUtc.AddSeconds(start);
    }

    public static IReadOnlyList<ResourceSample> Downsample(
        IReadOnlyList<ResourceSample> samples,
        int bucketSeconds)
    {
        if (samples.Count == 0 || bucketSeconds <= 10)
            return samples;

        return samples
            .GroupBy(s => BucketStartUtc(s.TimestampUtc, bucketSeconds))
            .OrderBy(g => g.Key)
            .Select(g => Aggregate(g.Key, g.ToArray()))
            .ToList();
    }

    public static ResourceSample Aggregate(DateTime bucketUtc, IReadOnlyList<ResourceSample> items)
    {
        var last = items[^1];
        return last with
        {
            TimestampUtc = bucketUtc,
            ActiveUsers = items.Max(s => s.ActiveUsers),
            ActiveSessions = items.Max(s => s.ActiveSessions),
            KestrelConnections = items.Max(s => s.KestrelConnections),
            CurrentHttpRequests = items.Max(s => s.CurrentHttpRequests),
            SqlInteractive = items.Max(s => s.SqlInteractive),
            SqlAnalytics = items.Max(s => s.SqlAnalytics),
            SqlOther = items.Max(s => s.SqlOther),
            RedisClients = items.Max(s => s.RedisClients),
            AnalyticsGateBusy = items.Max(s => s.AnalyticsGateBusy),
            AnalyticsGateLimit = items.Max(s => s.AnalyticsGateLimit),
            BackendAvailable = items.Any(s => s.BackendAvailable),
            BackendCpuCores = items.Average(s => s.BackendCpuCores),
            BackendRssMib = items.Average(s => s.BackendRssMib),
            RedisAvailable = items.Any(s => s.RedisAvailable),
            RedisCpuCores = items.Average(s => s.RedisCpuCores),
            RedisRssMib = items.Average(s => s.RedisRssMib),
            SqlAvailable = items.Any(s => s.SqlAvailable),
            SqlCpuCores = items.Average(s => s.SqlCpuCores),
            SqlRssMib = items.Average(s => s.SqlRssMib),
            GcHeapMib = items.Average(s => s.GcHeapMib),
            AllocRateBytesPerSecond = items.Average(s => s.AllocRateBytesPerSecond),
            Gen2Collections = items.Max(s => s.Gen2Collections),
            ThreadPoolBusy = items.Max(s => s.ThreadPoolBusy),
            ThreadPoolQueue = items.Max(s => s.ThreadPoolQueue),
            SqlRunnable = items.Max(s => s.SqlRunnable),
            RedisHitRatio = items.Average(s => s.RedisHitRatio),
            RedisOpsPerSec = items.Average(s => s.RedisOpsPerSec),
            RedisEvictedKeys = items.Max(s => s.RedisEvictedKeys),
            RedisFragmentation = items.Average(s => s.RedisFragmentation),
            SqlBatchRequestsPerSec = items.Average(s => s.SqlBatchRequestsPerSec),
            SqlBufferCacheHitRatio = items.Average(s => s.SqlBufferCacheHitRatio),
        };
    }
}
