using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class CompareChartBuilderTests
{
    [Fact]
    public void FromCompare_IicZScore_TakesTopByAbsDelta_SkipsTotals()
    {
        var compare = new VersionCompareResultDto(
            [
                new VersionCompareRowDto(0, null, "Итого", IsTotal: true, FactZScoreA: 1m, FactZScoreB: 2m, FactZDelta: 1m),
                new VersionCompareRowDto(10, null, "A", FactZScoreA: 0.1m, FactZScoreB: 0.2m, FactZDelta: 0.1m),
                new VersionCompareRowDto(11, null, "B", FactZScoreA: -1m, FactZScoreB: 2m, FactZDelta: 3m),
            ],
            [],
            []);

        var chart = CompareChartBuilder.FromCompare(compare, "vA", "vB", level: "iic", metric: "zScore", top: 1);

        chart.Level.Should().Be("iic");
        chart.VersionAName.Should().Be("vA");
        chart.Points.Should().HaveCount(1);
        chart.Points[0].Category.Should().Be("B");
        chart.Points[0].ValueA.Should().Be(-1m);
        chart.Points[0].Delta.Should().Be(3m);
    }

    [Fact]
    public void FromCompare_FacilityHours_UsesFactHoursAndDelta()
    {
        var compare = new VersionCompareResultDto(
            [],
            [
                new VersionCompareFacilityRowDto("1", "Объект", FactLaborHoursA: 10m, FactLaborHoursB: 4m, HoursDelta: -6m, ZScoreA: 1m, ZScoreB: 2m, ZDelta: 1m),
            ],
            []);

        var chart = CompareChartBuilder.FromCompare(compare, "A", "B", level: "facility", metric: "hours", top: 20);

        chart.Level.Should().Be("facility");
        chart.Points.Should().ContainSingle();
        chart.Points[0].ValueA.Should().Be(10m);
        chart.Points[0].ValueB.Should().Be(4m);
        chart.Points[0].Delta.Should().Be(-6m);
    }
}
