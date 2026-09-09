using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class IicClaimedBucketsTests
{
    [Fact]
    public void NearestClaimed_CarvesOutExplicitChild()
    {
        var parent = new Dictionary<int, int?>
        {
            [1] = null,
            [11] = 1,
            [12] = 1,
            [111] = 11,
        };
        var claimed = new HashSet<int> { 1, 11 };

        IicClaimedBuckets.NearestClaimed(12, claimed, parent).Should().Be(1);
        IicClaimedBuckets.NearestClaimed(11, claimed, parent).Should().Be(11);
        IicClaimedBuckets.NearestClaimed(111, claimed, parent).Should().Be(11);
    }

    [Fact]
    public void ApplyExclusiveVolumes_ParentDoesNotIncludeClaimedChild()
    {
        var parent = new Dictionary<int, int?>
        {
            [1] = null,
            [11] = 1,
            [12] = 1,
        };
        var claimed = new HashSet<int> { 1, 11 };
        var volumes = new Dictionary<int, decimal>
        {
            [1] = 10m,
            [11] = 4m,
            [12] = 6m,
        };

        var exclusive = IicClaimedBuckets.ApplyExclusiveVolumes(volumes, claimed, parent);

        exclusive[1].Should().Be(16m);
        exclusive[11].Should().Be(4m);
        exclusive.Should().NotContainKey(12);
    }

    [Fact]
    public void IsExclusive_RequiresNonEmptySet()
    {
        IicClaimedBuckets.IsExclusive(null).Should().BeFalse();
        IicClaimedBuckets.IsExclusive(new HashSet<int>()).Should().BeFalse();
        IicClaimedBuckets.IsExclusive(new HashSet<int> { 1 }).Should().BeTrue();
    }
}
