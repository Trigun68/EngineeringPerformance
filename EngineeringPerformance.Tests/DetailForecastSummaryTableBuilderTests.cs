using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class DetailForecastSummaryTableBuilderTests
{
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
        table.Columns[0].UnitName.Should().Be("м³");
        table.Columns[1].UnitName.Should().Be("т");
    }

    [Fact]
    public void Build_AggregatesByFacilityAndRoot_SplitsUnitsFromIicGroups()
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
            new("01", "Объект 1", 100, 10m, 1m, 100m),
            new("01", "Объект 1", 101, 5m, 2m, 50m),
            new("02", "Объект 2", 100, 3m, 1m, 30m),
        };

        var table = DetailForecastSummaryTableBuilder.Build(rows, directions, "P-01", "План v1");

        table.Columns.Should().HaveCount(2);
        table.Columns[0].BlockTitle.Should().Be("Бетон общий — м³");
        table.Columns[1].BlockTitle.Should().Be("Бетон общий — шт");

        table.Rows.Should().HaveCount(2);
        table.Rows[0].RowLabel.Should().Be("01 — Объект 1");
        table.Rows[0].Cells[0].PhysicalVolume.Should().Be(10m);
        table.Rows[0].Cells[0].LaborHours.Should().Be(100m);
        table.Rows[0].Cells[1].PhysicalVolume.Should().Be(10m);
        table.Rows[0].Cells[1].LaborHours.Should().Be(50m);

        table.Rows[1].Cells[0].PhysicalVolume.Should().Be(3m);
        table.Rows[1].Cells[0].LaborHours.Should().Be(30m);
        table.Rows[1].Cells[1].PhysicalVolume.Should().Be(0m);
        table.Rows[1].Cells[1].LaborHours.Should().Be(0m);
    }

    [Fact]
    public void Build_InheritsIicUnitFromParentGroup()
    {
        var directions = new List<IicDirectionNodeDto>
        {
            new(1, null, "IIC", "IIC", 0),
            new(10, 1, "Арматура", "ARM", 0, "т"),
            new(100, 10, "А500", "A500", 0),
        };

        var rows = new List<DetailForecastSummaryInputRow>
        {
            new("01", "Объект", 100, 2m, 1m, 20m),
        };

        var table = DetailForecastSummaryTableBuilder.Build(rows, directions, "P-01", "План v1");

        table.Columns.Should().ContainSingle();
        table.Columns[0].UnitName.Should().Be("т");
        table.Columns[0].BlockTitle.Should().Be("Арматура — т");
    }
}
