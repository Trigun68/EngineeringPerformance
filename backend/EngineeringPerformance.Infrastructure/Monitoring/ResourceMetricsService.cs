using EngineeringPerformance.Application.Monitoring;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ResourceMetricsService(
    ResourceMetricsStore store,
    ResourceSnapshotCollector collector,
    IResourceMetricHistoryStore history,
    IOptions<ResourceMonitoringOptions> options) : IResourceMetricsService
{
    public async Task<ResourceMetricsDto> GetResourceMetricsAsync(
        int windowMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var clamped = ResourceSeriesBucket.ClampWindowMinutes(windowMinutes);
        var bucketSeconds = ResourceSeriesBucket.SecondsForWindow(clamped);
        var now = DateTime.UtcNow;
        var from = now.AddMinutes(-clamped);

        var live = store.GetWindow(Math.Min(clamped, 60), now);
        if (live.Count == 0)
        {
            store.Add(await collector.CollectAsync(cancellationToken));
            live = store.GetWindow(Math.Min(clamped, 60), DateTime.UtcNow);
            now = DateTime.UtcNow;
        }

        var latest = live[^1];
        var seriesSamples = await ResolveSeriesAsync(clamped, bucketSeconds, from, now, live, latest, cancellationToken);
        var historyFrom = await history.GetOldestTimestampUtcAsync(cancellationToken)
            ?? seriesSamples.FirstOrDefault()?.TimestampUtc
            ?? latest.TimestampUtc;

        var sqlLimit = options.Value.SqlMemoryLimitMib;
        var snapshot = MapSnapshot(latest);
        var series = MapSeries(seriesSamples, sqlLimit);
        var verdicts = ResourceVerdictBuilder.Build(live.Select(s => s.ToVerdictInput()).ToList());
        return new ResourceMetricsDto(
            snapshot,
            series,
            verdicts,
            latest.TimestampUtc,
            clamped,
            bucketSeconds,
            historyFrom);
    }

    private async Task<IReadOnlyList<ResourceSample>> ResolveSeriesAsync(
        int windowMinutes,
        int bucketSeconds,
        DateTime fromUtc,
        DateTime nowUtc,
        IReadOnlyList<ResourceSample> live,
        ResourceSample latest,
        CancellationToken cancellationToken)
    {
        var memoryCovers = live.Count > 0
            && live[0].TimestampUtc <= fromUtc.AddSeconds(20)
            && windowMinutes <= 60;

        IReadOnlyList<ResourceSample> series;
        if (memoryCovers)
        {
            series = ResourceSampleDownsampler.Downsample(live, bucketSeconds);
        }
        else
        {
            var fromSql = await history.GetBucketedAsync(fromUtc, nowUtc, bucketSeconds, cancellationToken);
            series = fromSql.Count > 0
                ? fromSql
                : ResourceSampleDownsampler.Downsample(live, bucketSeconds);
        }

        if (series.Count == 0)
            return [latest];

        var last = series[^1];
        if (latest.TimestampUtc > last.TimestampUtc.AddSeconds(bucketSeconds / 2.0))
            return [.. series, latest];

        return series;
    }

    private static ResourceSnapshotDto MapSnapshot(ResourceSample latest)
        => new(
            latest.ActiveUsers,
            latest.ActiveSessions,
            latest.KestrelConnections,
            latest.CurrentHttpRequests,
            new SqlLaneConnectionsDto(
                latest.SqlInteractive,
                latest.SqlAnalytics,
                latest.SqlOther,
                latest.SqlConnectionsTotal),
            latest.RedisClients,
            latest.AnalyticsGateBusy,
            latest.AnalyticsGateLimit,
            new ServiceResourceDto(latest.BackendAvailable, "Process", latest.BackendCpuCores, latest.BackendRssMib),
            new ServiceResourceDto(latest.RedisAvailable, latest.RedisStatus, latest.RedisCpuCores, latest.RedisRssMib),
            new ServiceResourceDto(latest.SqlAvailable, latest.SqlStatus, latest.SqlCpuCores, latest.SqlRssMib),
            ResourceCost.PerUser(latest.BackendCpuCores, latest.ActiveUsers),
            ResourceCost.PerUser(latest.BackendRssMib, latest.ActiveUsers),
            new BackendRuntimeDto(
                latest.GcHeapMib,
                latest.AllocRateBytesPerSecond,
                latest.Gen2Collections,
                latest.ThreadPoolBusy,
                latest.ThreadPoolQueue,
                latest.SqlRunnable,
                latest.RedisHitRatio,
                latest.RedisOpsPerSec,
                latest.RedisEvictedKeys,
                latest.RedisFragmentation,
                latest.SqlBatchRequestsPerSec,
                latest.SqlBufferCacheHitRatio));

    private static ResourceSeriesDto MapSeries(IReadOnlyList<ResourceSample> samples, double sqlLimit)
        => new(
            ResourceMetricsStore.Map(samples, s => s.BackendCpuCores),
            ResourceMetricsStore.Map(samples, s => s.RedisCpuCores),
            ResourceMetricsStore.Map(samples, s => s.SqlCpuCores),
            ResourceMetricsStore.Map(samples, s => s.BackendRssMib),
            ResourceMetricsStore.Map(samples, s => s.RedisRssMib),
            ResourceMetricsStore.Map(samples, s => s.SqlRssMib),
            ResourceMetricsStore.Map(samples, s => s.ActiveUsers),
            ResourceMetricsStore.Map(samples, s => s.KestrelConnections),
            ResourceMetricsStore.Map(samples, s => s.SqlInteractive),
            ResourceMetricsStore.Map(samples, s => s.SqlAnalytics),
            ResourceMetricsStore.Map(samples, s => s.RedisClients),
            ResourceMetricsStore.Map(samples, s => s.ThreadPoolQueue),
            ResourceMetricsStore.Map(samples, s => s.SqlRunnable),
            ResourceMetricsStore.Map(samples, s => s.RedisHitRatio),
            ResourceMetricsStore.Map(samples, s => ResourceCpu.BytesToMib((long)s.AllocRateBytesPerSecond)),
            ResourceMetricsStore.Map(samples, s => s.Gen2Collections),
            sqlLimit);
}
