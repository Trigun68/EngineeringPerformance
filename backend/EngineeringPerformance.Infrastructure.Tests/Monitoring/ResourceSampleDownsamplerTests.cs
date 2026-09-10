using EngineeringPerformance.Infrastructure.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Monitoring;

public sealed class ResourceSampleDownsamplerTests
{
    [Fact]
    public void Downsample_ThirtySecondBuckets_AveragesCpuAndMaxUsers()
    {
        var t0 = ResourceSampleDownsampler.BucketStartUtc(
            new DateTime(2026, 9, 1, 12, 0, 5, DateTimeKind.Utc), 30);
        var samples = new[]
        {
            Sample(t0, cpu: 0.2, users: 1),
            Sample(t0.AddSeconds(10), cpu: 0.4, users: 3),
            Sample(t0.AddSeconds(20), cpu: 0.6, users: 2),
            Sample(t0.AddSeconds(30), cpu: 1.0, users: 4),
        };

        var buckets = ResourceSampleDownsampler.Downsample(samples, 30);

        buckets.Should().HaveCount(2);
        buckets[0].BackendCpuCores.Should().BeApproximately(0.4, 0.0001);
        buckets[0].ActiveUsers.Should().Be(3);
        buckets[1].BackendCpuCores.Should().Be(1.0);
        buckets[1].ActiveUsers.Should().Be(4);
    }

    [Fact]
    public void Downsample_NativeTenSeconds_ReturnsSameList()
    {
        var t0 = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var samples = new[] { Sample(t0, 0.1, 1), Sample(t0.AddSeconds(10), 0.2, 2) };
        ReferenceEquals(ResourceSampleDownsampler.Downsample(samples, 10), samples).Should().BeTrue();
    }

    private static ResourceSample Sample(DateTime timestampUtc, double cpu, int users)
        => new(
            timestampUtc,
            users,
            0, 0, 0, 0, 0, 0, 0, 0, 2,
            true, cpu, 100,
            true, "ok", 0, 10,
            true, "ok", 0, 200,
            50, 0, 1, 0, 0, 0, 0.9, 1, 0, 1, 0, 0.99);
}
