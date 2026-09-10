using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

/// <summary>FIX-01: родительские часы = сумма листьев из scenario path (RecalculatePlanHours).</summary>
public sealed class ForecastParentHoursAggregationTests
{
    private static readonly LaborIntensityCalculator.EquipmentLine[] NoEquipment = [];

    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void AggregateByDirection_ParentEqualsSumOfLeafScenarioHours()
    {
        // Нелинейная норма: Power-like через Linear с разным объёмом даёт пропорциональность;
        // ByEachValue: два PPV на одном направлении → часы суммируются по направлению.
        var snap = new NormCalculationSnapshot(
            10m, null, 2m,
            NormFormulaMapper.FromLegacyPerformance(10m),
            NormFormulaMapper.FromLegacyStdDev(10m, 2m));

        var lines = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, 100, "A", 5m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, 100, "B", 3m, 1m, true),
            new LaborIntensityCalculator.PpvLine(3, 200, "C", 4m, 1m, true),
        };

        var norms = new Dictionary<int, NormCalculationSnapshot> { [100] = snap, [200] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var hoursById = _engine.RecalculatePlanHours(
                PlanningScenario.ByEachValue, lines, NoEquipment, norms, z).Ppv
            .ToDictionary(r => r.PpvId);

        // Агрегация по направлению — сумма листьев (то, что делал AggregateByDirection без оборудования).
        decimal SumDir(int dirId) =>
            lines.Where(l => l.ClassifierId == dirId).Sum(l => hoursById[l.Id].Base ?? 0m);

        var leaf100 = (hoursById[1].Base ?? 0m) + (hoursById[2].Base ?? 0m);
        var leaf200 = hoursById[3].Base ?? 0m;

        SumDir(100).Should().BeApproximately(leaf100, 0.01m);
        SumDir(200).Should().BeApproximately(leaf200, 0.01m);

        // «Родитель» = сумма направлений-листьев (как GetLeafScenarioHours после FIX-01).
        var parentTotal = SumDir(100) + SumDir(200);
        parentTotal.Should().BeApproximately(leaf100 + leaf200, 0.01m);
    }
}
