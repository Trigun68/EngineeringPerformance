using EngineeringPerformance.Application.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Monitoring;

public sealed class MonitoringSelfTrafficTests
{
    [Theory]
    [InlineData("/api/monitoring/resource-metrics", true, true)]
    [InlineData("/swagger/index.html", true, true)]
    [InlineData("/api/health", false, true)]
    [InlineData("/metrics", false, true)]
    [InlineData("/api/projects", false, false)]
    public void ClassifiesSelfTraffic(string path, bool red, bool presence)
    {
        MonitoringSelfTraffic.ExcludeFromRed(path).Should().Be(red);
        MonitoringSelfTraffic.ExcludeFromPresence(path).Should().Be(presence);
    }
}
