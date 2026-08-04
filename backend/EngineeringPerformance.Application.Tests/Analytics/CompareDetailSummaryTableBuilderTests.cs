using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class CompareDetailSummaryTableBuilderTests
{
    [Fact]
    public void Build_OrdersRowsAndPreservesBaseAndFactMetrics()
    {
        var rows = new[]
        {
            new VersionCompareDetailRowDto(
                "02", "Объект 2", "", "PV-2", "Позиция 2", "W-2", "Работа 2",
                QuantityA: 20m, QuantityB: 22m, PlannedLaborHoursA: 40m, PlannedLaborHoursB: 44m,
                FactLaborHoursA: 41m, FactLaborHoursB: 43m, PlannedProductivityA: 0.5m,
                PlannedProductivityB: 0.55m, FactProductivityA: 0.49m, FactProductivityB: 0.51m,
                ZScoreA: -0.1m, ZScoreB: 0.2m, IicGroupName: "Группа B", Presence: "A+B"),
            new VersionCompareDetailRowDto(
                "01", "Объект 1", "", "PV-1", "Позиция 1", "W-1", "Работа 1",
                QuantityA: 10m, QuantityB: 12m, PlannedLaborHoursA: 20m, PlannedLaborHoursB: 24m,
                FactLaborHoursA: 21m, FactLaborHoursB: 23m, PlannedProductivityA: 0.5m,
                PlannedProductivityB: 0.5m, FactProductivityA: 0.48m, FactProductivityB: 0.52m,
                ZScoreA: -0.2m, ZScoreB: 0.1m, IicGroupName: "Группа A", Presence: "A+B"),
        };

        var table = CompareDetailSummaryTableBuilder.Build(rows);

        table.Select(row => row.FacilityCode).Should().Equal("01", "02");
        table[0].PlannedLaborHoursA.Should().Be(20m);
        table[0].FactLaborHoursB.Should().Be(23m);
        table[0].FactProductivityA.Should().Be(0.48m);
        table[0].ZScoreB.Should().Be(0.1m);
    }
}
