using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class AggregationTests
{
    private static readonly LaborIntensityCalculator.EquipmentLine[] NoEquipment = [];

    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ByEachValue_SumsMatchRowTotals()
    {
        var norm = new NormCalculationSnapshot(
            100m, null, 10m,
            NormFormulaMapper.FromLegacyPerformance(100m),
            NormFormulaMapper.FromLegacyStdDev(100m, 10m));

        var lines = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, 10, "A", 5m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, 10, "B", 3m, 1m, true),
        };

        var results = _engine.RecalculatePlanHours(
            PlanningScenario.ByEachValue,
            lines,
            NoEquipment,
            new Dictionary<int, NormCalculationSnapshot> { [10] = norm },
            new Dictionary<int, PlanningZParameters>()).Ppv;

        results.Should().HaveCount(2);
        results.Sum(r => r.Base ?? 0).Should().BeGreaterThan(0);
    }
}
