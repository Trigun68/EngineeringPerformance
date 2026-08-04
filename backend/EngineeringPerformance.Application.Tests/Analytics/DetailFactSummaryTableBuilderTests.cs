using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class DetailFactSummaryTableBuilderTests
{
    [Fact]
    public void Build_AggregatesFoPlanFactAndWeightedZ()
    {
        var directions = new List<IicDirectionNodeDto>
        {
            new(1, null, "IIC", "IIC", 0),
            new(10, 1, "Бетон общий", "BET", 0, "м³"),
            new(100, 10, "Бетон B25", "B25", 0, "м³"),
        };

        var rows = new List<DetailFactSummaryInputRow>
        {
            new("01", "Объект 1", 100, 10m, 1m, 50m, 40m, -0.5m),
            new("01", "Объект 1", 100, 5m, 1m, 25m, 20m, 0.5m),
            new("02", "Объект 2", 100, 3m, 1m, 15m, null, null),
        };

        var table = DetailFactSummaryTableBuilder.Build(rows, directions, "P-01", "Факт v1");

        table.Columns.Should().ContainSingle();
        table.Rows.Should().HaveCount(2);

        var r0 = table.Rows[0].Cells[0];
        r0.PhysicalVolume.Should().Be(15m);
        r0.PlanHours.Should().Be(75m);
        r0.FactHours.Should().Be(60m);
        // weighted Z: (-0.5*40 + 0.5*20) / 60 = -10/60
        r0.ZScore.Should().BeApproximately(-0.1667m, 0.001m);
        table.Rows[0].FactLaborHoursTotal.Should().Be(60m);

        var r1 = table.Rows[1].Cells[0];
        r1.PhysicalVolume.Should().Be(3m);
        r1.PlanHours.Should().Be(15m);
        r1.FactHours.Should().Be(0m);
        r1.ZScore.Should().BeNull();
        table.Rows[1].FactLaborHoursTotal.Should().Be(0m);
    }
}
