using EngineeringPerformance.Application.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class PerformanceReportInsightsBuilderTests
{
    [Fact]
    public void Build_RanksTopNegativeAndPositive_ByAbsZTimesHours()
    {
        var directions = new List<PerformanceReportMergedDirectionRowDto>
        {
            Merged(1, null, 0, "ROOT", "Корень", z: null, hours: 100, volume: 10),
            Merged(2, 1, 1, "A", "Плохое тяжёлое", z: 1.5m, hours: 100, volume: 50),
            Merged(3, 1, 1, "B", "Плохое лёгкое", z: 2.0m, hours: 10, volume: 5),
            Merged(4, 1, 1, "C", "Хорошее тяжёлое", z: -1.2m, hours: 80, volume: 40),
            Merged(5, 1, 1, "D", "Хорошее лёгкое", z: -2.0m, hours: 5, volume: 2),
        };

        var insights = PerformanceReportInsightsBuilder.Build(directions);

        insights.TopNegative.Should().HaveCount(2);
        insights.TopNegative[0].Name.Should().Be("Плохое тяжёлое"); // |1.5|*100 = 150 > |2|*10 = 20
        insights.TopPositive[0].Name.Should().Be("Хорошее тяжёлое"); // |-1.2|*80 = 96 > |-2|*5 = 10
        insights.Items.Should().Contain(i => i.Kind == "risk");
        insights.Items.Should().Contain(i => i.Kind == "strength");
        insights.Items.Should().NotContain(i => i.Kind == "data");
    }

    [Fact]
    public void Build_EmptyDirections_ReturnsEmptyInsights()
    {
        var insights = PerformanceReportInsightsBuilder.Build([]);
        insights.Items.Should().BeEmpty();
        insights.TopNegative.Should().BeEmpty();
        insights.TopPositive.Should().BeEmpty();
    }

    private static PerformanceReportMergedDirectionRowDto Merged(
        int id,
        int? parentId,
        int level,
        string code,
        string name,
        decimal? z,
        decimal hours,
        decimal volume) =>
        new(
            WorkDirectionId: id,
            ParentId: parentId,
            Level: level,
            Code: code,
            Name: name,
            UnitName: "ч",
            PhysicalVolume: volume,
            PlanLaborHours: hours,
            FactLaborHours: hours,
            PlanSubtreeHours: hours,
            FactSubtreeHours: hours,
            PlanProductivity: null,
            FactProductivity: null,
            NormProductivity: null,
            NormStdDev: null,
            PlanZScore: z,
            FactZScore: z,
            PlanInterpretation: null,
            FactInterpretation: null);
}
