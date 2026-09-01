using EngineeringPerformance.Infrastructure.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Monitoring;

public sealed class RedisInfoParserTests
{
    [Fact]
    public void ParseRaw_ReadsMemoryCpuAndHitRate()
    {
        const string info = """
            # Memory
            used_memory:1048576
            used_memory_rss:2097152
            mem_fragmentation_ratio:1.25
            evicted_keys:3
            # Clients
            connected_clients:4
            # Stats
            instantaneous_ops_per_sec:12
            keyspace_hits:90
            keyspace_misses:10
            # CPU
            used_cpu_sys:1.5
            used_cpu_user:2.5
            """;

        var parsed = RedisInfoParser.ParseRaw(info);

        parsed.UsedMemoryRssBytes.Should().Be(2097152);
        parsed.ConnectedClients.Should().Be(4);
        parsed.UsedCpuSeconds.Should().Be(4.0);
        parsed.HitRatio.Should().BeApproximately(0.9, 0.0001);
        parsed.EvictedKeys.Should().Be(3);
        parsed.FragmentationRatio.Should().BeApproximately(1.25, 0.0001);
    }

    [Fact]
    public void Parse_FallsBackToUsedMemoryWhenRssMissing()
    {
        var parsed = RedisInfoParser.Parse(new Dictionary<string, string>
        {
            ["used_memory"] = "4096",
        });

        parsed.UsedMemoryRssBytes.Should().Be(4096);
    }
}
