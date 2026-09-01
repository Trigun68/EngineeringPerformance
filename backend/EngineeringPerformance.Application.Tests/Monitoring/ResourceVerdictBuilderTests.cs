using EngineeringPerformance.Application.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Monitoring;

public sealed class ResourceVerdictBuilderTests
{
    [Fact]
    public void EmptySamples_AsksToWait()
    {
        var lines = ResourceVerdictBuilder.Build([]);
        lines.Should().ContainSingle(l => l.Contains("подождите", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShareLine_NamesSqlAsLargestRamConsumer()
    {
        var sample = Base(sqlRss: 1500, backendRss: 200, redisRss: 80);
        var lines = ResourceVerdictBuilder.Build([sample]);
        lines.Should().Contain(l => l.StartsWith("RAM:", StringComparison.Ordinal) && l.Contains("SQL"));
        lines.Should().Contain(l => l.StartsWith("CPU:", StringComparison.Ordinal));
    }

    [Fact]
    public void GateFull_AddsAnalyticsQueueLine()
    {
        var sample = Base() with { AnalyticsGateBusy = 2, AnalyticsGateLimit = 2 };
        ResourceVerdictBuilder.Build([sample]).Should().Contain(l => l.Contains("AnalyticsSqlGate"));
    }

    [Fact]
    public void SqlConnectionsGrowUsersFlat_FlagsLeak()
    {
        var samples = Enumerable.Range(0, 6)
            .Select(i => Base(sqlConnections: 10 + i, users: 3) with
            {
                TimestampUtc = DateTime.UtcNow.AddSeconds(i * 10),
            })
            .ToList();

        ResourceVerdictBuilder.Build(samples).Should().Contain(l => l.Contains("утечку SQL"));
    }

    [Fact]
    public void AllocRateSpike_FlagsGcPressure()
    {
        var samples = Enumerable.Range(0, 5)
            .Select(i => Base() with
            {
                TimestampUtc = DateTime.UtcNow.AddSeconds(i * 10),
                AllocRateBytesPerSecond = i == 4 ? 40 * 1024 * 1024 : 5 * 1024 * 1024,
            })
            .ToList();

        ResourceVerdictBuilder.Build(samples).Should().Contain(l => l.Contains("GC"));
    }

    private static ResourceVerdictInput Base(
        int users = 2,
        int sqlConnections = 4,
        double sqlRss = 100,
        double backendRss = 50,
        double redisRss = 10)
        => new(
            DateTime.UtcNow,
            users,
            sqlConnections,
            AnalyticsGateBusy: 0,
            AnalyticsGateLimit: 2,
            BackendCpuCores: 0.2,
            RedisCpuCores: 0.05,
            SqlCpuCores: 0.4,
            BackendRssMib: backendRss,
            RedisRssMib: redisRss,
            SqlRssMib: sqlRss,
            AllocRateBytesPerSecond: 1_000_000,
            Gen2Collections: 3);
}
