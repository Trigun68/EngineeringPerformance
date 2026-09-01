using EngineeringPerformance.Application.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class ResourceMetricsSampler(
    ResourceSnapshotCollector collector,
    ResourceMetricsStore store,
    IResourceMetricHistoryStore history,
    IOptions<ResourceMonitoringOptions> options,
    ILogger<ResourceMetricsSampler> logger) : BackgroundService
{
    private DateTime _lastPruneUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        store.ConfigureRetention(TimeSpan.FromMinutes(Math.Clamp(options.Value.RetentionMinutes, 15, 360)));
        var interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.SampleIntervalSeconds, 5, 60));
        using var timer = new PeriodicTimer(interval);

        await SampleOnceAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await SampleOnceAsync(stoppingToken);
    }

    private async Task SampleOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sample = await collector.CollectAsync(cancellationToken);
            store.Add(sample);
            await history.AppendAsync(sample, cancellationToken);
            await MaybePruneAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Сэмпл ресурсов сервера не записан");
        }
    }

    private async Task MaybePruneAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (now - _lastPruneUtc < TimeSpan.FromHours(1))
            return;

        _lastPruneUtc = now;
        var days = ResourceSeriesBucket.ClampHistoryRetentionDays(options.Value.HistoryRetentionDays);
        await history.PruneOlderThanAsync(now.AddDays(-days), cancellationToken);
    }
}
