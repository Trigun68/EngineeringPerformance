using EngineeringPerformance.Infrastructure.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Monitoring;

public sealed class KestrelTcpConnectionsTests
{
    [Fact]
    public void CountEstablished_DoesNotThrow()
    {
        var count = KestrelTcpConnections.CountEstablished();
        count.Should().BeGreaterThanOrEqualTo(0);
    }
}
