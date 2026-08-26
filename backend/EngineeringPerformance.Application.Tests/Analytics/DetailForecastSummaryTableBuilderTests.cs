using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class DetailForecastSummaryTableBuilderTests
{
    [Fact]
    public void Build_AggregatesByFacilityAndRoot_RoundsToWholeNumbers()
    {
        var directions = new List<IicDirectionNodeDto>
        {
            new(1, null, "IIC", "IIC", 0),
            new(10, 1, "Бетон общий", "BET", 0),
            new(100, 10, "Бетон B25", "B25", 0, "м³"),
            new(101, 10, "Бетон B30", "B30", 1, "шт"),
        };

        var rows = new List<DetailForecastSummaryInputRow>
        {
            new("01", "Объект 1", 100, 10.4m, 1m, 100.4m),
            new("01", "Объект 1", 101, 5m, 2m, 50.5m),
            new("02", "Объект 2", 100, 3.2m, 1m, 30.2m),
        };

        var table = DetailForecastSummaryTableBuilder.Build(rows, directions, "P-01", "План v1");

        table.Columns.Should().HaveCount(2);
        table.Columns.Select(c => c.BlockTitle).Should().Equal("BET — Бетон общий (м³)", "BET — Бетон общий (шт)");
        table.Rows.Should().HaveCount(2);

        table.Rows[0].Cells[0].PhysicalVolume.Should().Be(10m);
        table.Rows[0].Cells[0].LaborHours.Should().Be(100m);
        table.Rows[0].Cells[1].PhysicalVolume.Should().Be(10m);
        table.Rows[0].Cells[1].LaborHours.Should().Be(51m);
        table.Rows[0].LaborHoursTotal.Should().Be(151m);

        table.Rows[1].Cells[0].PhysicalVolume.Should().Be(3m);
        table.Rows[1].Cells[0].LaborHours.Should().Be(30m);
        table.Rows[1].LaborHoursTotal.Should().Be(30m);
    }

    [Fact]
    public void Build_ZeroOrNegativeCoefficient_UsesOne()
    {
        var directions = new List<IicDirectionNodeDto>
        {
            new(1, null, "IIC", "IIC", 0),
            new(10, 1, "Группа", "G", 0, "шт"),
            new(100, 10, "Позиция", "P", 0, "шт"),
        };

        var rows = new List<DetailForecastSummaryInputRow>
        {
            new("01", "Объект", 100, 8m, 0m, 16m),
            new("01", "Объект", 100, 2m, -1m, 4m),
        };

        var table = DetailForecastSummaryTableBuilder.Build(rows, directions, "P-01", "v1");

        table.Rows.Should().ContainSingle();
        table.Rows[0].Cells[0].PhysicalVolume.Should().Be(10m);
        table.Rows[0].Cells[0].LaborHours.Should().Be(20m);
    }

    [Fact]
    public void Build_UsesChildrenOfSingleMetaRoot_AsBlockColumns()
    {
        var directions = new List<IicDirectionNodeDto>
        {
            new(1, null, "IIC", "IIC", 0),
            new(10, 1, "Бетон", "B1", 0, "м³"),
            new(11, 1, "Арматура", "B2", 1, "т"),
            new(100, 10, "Бетон B25", "B25", 0, "м³"),
            new(200, 11, "А500", "A500", 0, "т"),
        };

        var rows = new List<DetailForecastSummaryInputRow>
        {
            new("01", "Объект", 100, 1m, 1m, 10m),
            new("01", "Объект", 200, 2m, 1m, 20m),
        };

        var table = DetailForecastSummaryTableBuilder.Build(rows, directions, "P-01", "План v1");

        table.Columns.Should().HaveCount(2);
        table.Columns.Select(c => c.RootName).Should().Equal("Бетон", "Арматура");
        table.Columns.Select(c => c.BlockTitle).Should().Equal("B1 — Бетон (м³)", "B2 — Арматура (т)");
    }
}
