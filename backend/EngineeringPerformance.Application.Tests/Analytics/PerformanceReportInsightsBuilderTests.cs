using EngineeringPerformance.Application.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class PerformanceReportInsightsBuilderTests
{
    [Fact]
    public void Build_RanksTopNegativeAndPositive_ByAbsZTimesHours()
    {
        var directions = new List<PerformanceReportDirectionRowDto>
        {
            Dir(1, null, 0, "ROOT", "Корень", z: null, hours: 100),
            Dir(2, 1, 1, "A", "Плохое тяжёлое", z: 1.5m, hours: 100),
            Dir(3, 1, 1, "B", "Плохое лёгкое", z: 2.0m, hours: 10),
            Dir(4, 1, 1, "C", "Хорошее тяжёлое", z: -1.2m, hours: 80),
            Dir(5, 1, 1, "D", "Хорошее лёгкое", z: -2.0m, hours: 5),
        };

        var insights = PerformanceReportInsightsBuilder.Build(directions, 200, 0, 90);

        insights.TopNegative.Should().HaveCount(2);
        insights.TopNegative[0].Name.Should().Be("Плохое тяжёлое"); // |1.5|*100 = 150 > |2|*10 = 20
        insights.TopPositive[0].Name.Should().Be("Хорошее тяжёлое"); // |-1.2|*80 = 96 > |-2|*5 = 10
        insights.Items.Should().Contain(i => i.Kind == "risk");
        insights.Items.Should().Contain(i => i.Kind == "strength");
    }

    [Fact]
    public void Build_AddsAccuracyInsight_WhenBelow85()
    {
        var insights = PerformanceReportInsightsBuilder.Build([], 100, 0, 70);
        insights.Items.Should().Contain(i => i.Kind == "data" && i.Title.Contains("Точность"));
    }

    private static PerformanceReportDirectionRowDto Dir(
        int id,
        int? parentId,
        int level,
        string code,
        string name,
        decimal? z,
        decimal hours) =>
        new(
            WorkDirectionId: id,
            ParentId: parentId,
            Level: level,
            Code: code,
            Name: name,
            UnitName: "ч",
            FactPhysicalVolume: 1,
            FactLaborHours: hours,
            SubtreeHours: hours,
            FactProductivity: null,
            NormProductivity: null,
            NormStdDev: null,
            ZScore: z,
            Interpretation: null);
}
