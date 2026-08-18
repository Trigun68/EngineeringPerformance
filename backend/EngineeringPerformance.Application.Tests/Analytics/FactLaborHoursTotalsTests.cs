using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class FactLaborHoursTotalsTests
{
    [Fact]
    public void FromActualHours_IgnoresPlannedHours_WhenActualIsZero()
    {
        var labor = new List<ProjectLaborIntensity>
        {
            new() { PlannedHours = 25310m, ActualHours = 0m },
            new() { PlannedHours = 100m, ActualHours = 0m },
        };

        FactLaborHoursTotals.FromActualHours(labor).Should().BeNull();
        FactLaborHoursTotals.FromLaborIntensities(labor).Should().Be(25410m);
    }

    [Fact]
    public void FromActualHours_SumsOnlyActual()
    {
        var labor = new List<ProjectLaborIntensity>
        {
            new() { PlannedHours = 1000m, ActualHours = 40m },
            new() { PlannedHours = 500m, ActualHours = 10.5m },
        };

        FactLaborHoursTotals.FromActualHours(labor).Should().Be(50.5m);
    }
}
