using EngineeringPerformance.Application.Monitoring;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Monitoring;

public sealed class ResourceCostTests
{
    [Fact]
    public void PerUser_ZeroUsers_ReturnsNull()
    {
        ResourceCost.PerUser(1.5d, 0).Should().BeNull();
    }

    [Fact]
    public void PerUser_DividesByActiveUsers()
    {
        ResourceCost.PerUser(2d, 4).Should().Be(0.5d);
    }
}
