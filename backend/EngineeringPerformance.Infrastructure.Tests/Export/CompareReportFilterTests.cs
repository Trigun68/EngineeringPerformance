using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class CompareReportFilterTests
{
    [Fact]
    public void FilterFacility_HidesRows_WhenBothFactHoursNull()
    {
        var rows = new List<VersionCompareFacilityRowDto>
        {
            new("F1", "A", FactLaborHoursA: null, FactLaborHoursB: null, ZScoreA: 1m, ZScoreB: 2m),
            new("F2", "B", FactLaborHoursA: 10m, FactLaborHoursB: 12m, ZScoreA: 0m, ZScoreB: 0m),
        };

        var filtered = CompareReportFilter.FilterFacility(rows, onlyDifferences: true);

        filtered.Should().ContainSingle(r => r.FacilityCode == "F2");
    }

    [Fact]
    public void FilterDetail_IgnoresPlannedLaborDifferences()
    {
        var rows = new List<VersionCompareDetailRowDto>
        {
            new(
                "F", "N", "DS", "PV", "PN", "W", "WN",
                QuantityA: 1m,
                QuantityB: 1m,
                PlannedLaborHoursA: 1m,
                PlannedLaborHoursB: 999m,
                FactLaborHoursA: 5m,
                FactLaborHoursB: 5m,
                FactProductivityA: 1m,
                FactProductivityB: 1m,
                Presence: "A+B"),
        };

        CompareReportFilter.FilterDetail(rows, onlyDifferences: true).Should().BeEmpty();
    }

    [Fact]
    public void FilterSummary_KeepsAncestors_WhenChildDiffers()
    {
        var rows = new List<VersionCompareRowDto>
        {
            new(1, null, "Root", FactZScoreA: 0m, FactZScoreB: 0m, PlanZScoreA: 0m, PlanZScoreB: 0m,
                PlannedLaborHoursA: 1m, PlannedLaborHoursB: 1m),
            new(2, 1, "Child", FactZScoreA: 0m, FactZScoreB: 1m, PlanZScoreA: 0m, PlanZScoreB: 0m,
                PlannedLaborHoursA: 1m, PlannedLaborHoursB: 1m),
        };

        var filtered = CompareReportFilter.FilterSummary(rows, onlyDifferences: true);
        filtered.Select(r => r.WorkDirectionId).Should().BeEquivalentTo([1, 2]);
    }
}
