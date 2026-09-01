using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Analytics;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ResourceSnapshotCollector(
    ProcessResourceCollector process,
    RedisResourceCollector redis,
    SqlResourceCollector sql,
    IActiveUserTracker users,
    AnalyticsSqlGate gate,
    IOptions<ResourceMonitoringOptions> options)
{
    private readonly SemaphoreSlim _sync = new(1, 1);

    public async Task<ResourceSample> CollectAsync(CancellationToken cancellationToken)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var window = TimeSpan.FromMinutes(Math.Max(1, options.Value.ActiveUserWindowMinutes));
            var processSnap = process.Capture(now);
            var redisTask = redis.CaptureAsync(now, cancellationToken);
            var sqlTask = sql.CaptureAsync(now, cancellationToken);
            await Task.WhenAll(redisTask, sqlTask);
            var redisSnap = await redisTask;
            var sqlSnap = await sqlTask;

            return new ResourceSample(
                now,
                users.CountActive(window, now),
                sqlSnap.ActiveSessions,
                processSnap.KestrelConnections,
                processSnap.CurrentHttpRequests,
                sqlSnap.Interactive,
                sqlSnap.Analytics,
                sqlSnap.Other,
                redisSnap.ConnectedClients,
                gate.BusyCount,
                gate.Limit,
                BackendAvailable: true,
                processSnap.CpuCores,
                processSnap.RssMib,
                redisSnap.Available,
                redisSnap.Status,
                redisSnap.CpuCores,
                redisSnap.RssMib,
                sqlSnap.Available,
                sqlSnap.Status,
                sqlSnap.CpuCores,
                sqlSnap.RssMib,
                processSnap.GcHeapMib,
                processSnap.AllocRateBytesPerSecond,
                processSnap.Gen2Collections,
                processSnap.ThreadPoolBusy,
                processSnap.ThreadPoolQueue,
                sqlSnap.Runnable,
                redisSnap.HitRatio,
                redisSnap.OpsPerSec,
                redisSnap.EvictedKeys,
                redisSnap.Fragmentation,
                sqlSnap.BatchRequestsPerSec,
                sqlSnap.BufferCacheHitRatio);
        }
        finally
        {
            _sync.Release();
        }
    }
}
