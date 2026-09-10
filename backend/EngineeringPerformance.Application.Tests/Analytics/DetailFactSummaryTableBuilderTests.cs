using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class DetailFactSummaryTableBuilderTests
{
    private static readonly IPerformanceCalculationEngine Engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

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
            new("01", "Объект 1", 100, 10m, 1m, 60m, 50m, 40m, 40m, -0.5m),
            new("01", "Объект 1", 100, 5m, 1m, 30m, 25m, 20m, 20m, 0.5m),
            new("02", "Объект 2", 100, 3m, 1m, 18m, 15m, 12m, null, null),
        };

        var table = DetailFactSummaryTableBuilder.Build(rows, directions, "P-01", "Факт v1", Engine);

        table.Columns.Should().ContainSingle();
        table.Rows.Should().HaveCount(2);

        var r0 = table.Rows[0].Cells[0];
        r0.PhysicalVolume.Should().Be(15m);
        r0.PessimisticHours.Should().Be(90m);
        r0.BaseHours.Should().Be(75m);
        r0.OptimisticHours.Should().Be(60m);
        r0.FactHours.Should().Be(60m);
        // weighted Z: (-0.5*40 + 0.5*20) / 60 = -10/60
        r0.ZScore.Should().BeApproximately(-0.1667m, 0.001m);
        table.Rows[0].FactLaborHoursTotal.Should().Be(60m);
        table.Rows[0].OptimisticLaborHoursTotal.Should().Be(60m);
        table.Rows[0].BaseLaborHoursTotal.Should().Be(75m);
        table.Rows[0].PessimisticLaborHoursTotal.Should().Be(90m);

        var r1 = table.Rows[1].Cells[0];
        r1.PhysicalVolume.Should().Be(3m);
        r1.PessimisticHours.Should().Be(18m);
        r1.BaseHours.Should().Be(15m);
        r1.OptimisticHours.Should().Be(12m);
        r1.FactHours.Should().Be(0m);
        r1.ZScore.Should().BeNull();
        table.Rows[1].FactLaborHoursTotal.Should().Be(0m);
    }
}
