using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class WeightedZScoreCalculatorTests
{
    [Fact]
    public void FromRootPerformanceRows_UsesSubtreeHours_OnRootsOnly()
    {
        var rows = new List<PerformanceRowDto>
        {
            new(10, "Root A", "A", null, 0, "ч", 100m, 50m, null, null, 2m, null, null, 1m, null, SubtreeHours: 50m),
            new(11, "Child", "B", 10, 1, "ч", 50m, 25m, null, null, 2m, null, null, 0.5m, null, SubtreeHours: 25m),
            new(20, "Root B", "C", null, 0, "ч", 80m, 30m, null, null, 3m, null, null, -0.5m, null, SubtreeHours: 30m),
        };

        var weighted = WeightedZScoreCalculator.FromRootPerformanceRows(rows, decimalPlaces: 4);

        // (1*50 + (-0.5)*30) / (50+30) = 35/80 = 0.4375
        weighted.Should().Be(0.4375m);
    }

    [Fact]
    public void FromRootPerformanceRows_NoEligibleRoots_ReturnsNull()
    {
        var rows = new List<PerformanceRowDto>
        {
            new(10, "Root", "A", null, 0, "ч", null, null, null, null, null, null, null, null, null, SubtreeHours: null),
        };

        WeightedZScoreCalculator.FromRootPerformanceRows(rows).Should().BeNull();
    }
}
