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
            Merged(1, null, 0, "A", "Плохое тяжёлое", z: 1.5m, hours: 100, volume: 50),
            Merged(2, null, 0, "B", "Плохое лёгкое", z: 2.0m, hours: 10, volume: 5),
            Merged(3, null, 0, "C", "Хорошее тяжёлое", z: -1.2m, hours: 80, volume: 40),
            Merged(4, null, 0, "D", "Хорошее лёгкое", z: -2.0m, hours: 5, volume: 2),
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


    [Fact]
    public void Build_UsesPlanZ_WhenFactZMissing()
    {
        var directions = new List<PerformanceReportMergedDirectionRowDto>
        {
            new(
                WorkDirectionId: 2,
                ParentId: null,
                Level: 0,
                Code: "A",
                Name: "План-лист",
                UnitName: "ч",
                PhysicalVolume: 10m,
                PlanLaborHours: 100m,
                FactLaborHours: null,
                PlanSubtreeHours: 100m,
                FactSubtreeHours: null,
                PlanProductivity: null,
                FactProductivity: null,
                NormProductivity: 1m,
                NormStdDev: 0.1m,
                PlanZScore: -0.4m,
                FactZScore: null,
                PlanInterpretation: null,
                FactInterpretation: null),
        };

        var insights = PerformanceReportInsightsBuilder.Build(directions);
        insights.Items.Should().NotBeEmpty();
        insights.TopPositive.Should().ContainSingle(r => r.Name == "План-лист");
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
            SubtreePhysicalVolume: volume,
            PlanProductivity: null,
            FactProductivity: null,
            NormProductivity: null,
            NormStdDev: null,
            PlanZScore: z,
            FactZScore: z,
            PlanInterpretation: null,
            FactInterpretation: null);
}
