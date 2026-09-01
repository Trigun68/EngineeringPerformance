using EngineeringPerformance.Application.Monitoring;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class RedisResourceCollector(
    IConnectionMultiplexer? multiplexer,
    ILogger<RedisResourceCollector> logger)
{
    private double _previousCpuSeconds;
    private DateTime _previousUtc = DateTime.MinValue;
    private bool _hasBaseline;

    internal async Task<RedisSnapshot> CaptureAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (multiplexer is null || !multiplexer.IsConnected)
        {
            return RedisSnapshot.Unavailable(multiplexer is null ? "Not configured" : "Disconnected");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            var server = multiplexer.GetEndPoints()
                .Select(ep => multiplexer.GetServer(ep))
                .FirstOrDefault(s => s.IsConnected);

            if (server is null)
                return RedisSnapshot.Unavailable("No connected endpoint");

            var sections = await server.InfoAsync().WaitAsync(timeout.Token);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in sections)
            {
                foreach (var pair in section)
                    map[pair.Key] = pair.Value;
            }

            var parsed = RedisInfoParser.Parse(map);
            var cpuCores = 0d;
            if (_hasBaseline)
                cpuCores = ResourceCpu.CoresFromCpuSeconds(_previousCpuSeconds, parsed.UsedCpuSeconds, nowUtc - _previousUtc);

            _previousCpuSeconds = parsed.UsedCpuSeconds;
            _previousUtc = nowUtc;
            _hasBaseline = true;

            return new RedisSnapshot(
                true,
                "Connected",
                cpuCores,
                ResourceCpu.BytesToMib(parsed.UsedMemoryRssBytes),
                parsed.ConnectedClients,
                parsed.HitRatio,
                parsed.InstantaneousOpsPerSec,
                parsed.EvictedKeys,
                parsed.FragmentationRatio);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось снять Redis INFO для вкладки ресурсов");
            return RedisSnapshot.Unavailable(ex.Message);
        }
    }
}

internal sealed record RedisSnapshot(
    bool Available,
    string Status,
    double CpuCores,
    double RssMib,
    int ConnectedClients,
    double HitRatio,
    double OpsPerSec,
    long EvictedKeys,
    double Fragmentation)
{
    public static RedisSnapshot Unavailable(string status)
        => new(false, status, 0, 0, 0, 0, 0, 0, 0);
}
