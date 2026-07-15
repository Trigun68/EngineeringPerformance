using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

/// <summary>FIX-01: родительские часы = сумма листьев из scenario path (LaborIntensityCalculator).</summary>
public sealed class ForecastParentHoursAggregationTests
{
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

        var calculator = new DeviationCalculator(new NormCalculator());
        var norms = new Dictionary<int, NormCalculationSnapshot> { [100] = snap, [200] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppvHours = ForecastHoursAggregator.ComputePpvHours(
            PlanningScenario.ByEachValue, lines, norms, z, calculator);

        var byDirection = ForecastHoursAggregator.AggregateByDirection(
            lines, ppvHours, Array.Empty<(int, decimal)>(), norms, z, calculator);

        var leaf100 = (ppvHours[1].Base ?? 0m) + (ppvHours[2].Base ?? 0m);
        var leaf200 = ppvHours[3].Base ?? 0m;

        byDirection[100].Base.Should().BeApproximately(leaf100, 0.01m);
        byDirection[200].Base.Should().BeApproximately(leaf200, 0.01m);

        // «Родитель» = сумма направлений-листьев (как GetLeafScenarioHours после FIX-01).
        var parentTotal = (byDirection[100].Base ?? 0m) + (byDirection[200].Base ?? 0m);
        parentTotal.Should().BeApproximately(leaf100 + leaf200, 0.01m);
    }
}
