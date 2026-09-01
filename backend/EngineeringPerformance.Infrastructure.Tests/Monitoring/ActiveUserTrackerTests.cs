using EngineeringPerformance.Infrastructure.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Monitoring;

public sealed class ActiveUserTrackerTests
{
    [Fact]
    public void CountActive_CountsDistinctUsersInsideWindow()
    {
        var tracker = new ActiveUserTracker();
        var now = DateTime.UtcNow;
        tracker.Record(1, now.AddMinutes(-1));
        tracker.Record(1, now.AddSeconds(-10));
        tracker.Record(2, now.AddMinutes(-10));

        tracker.CountActive(TimeSpan.FromMinutes(5), now).Should().Be(1);
    }
}
