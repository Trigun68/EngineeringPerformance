using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

public sealed class EquipmentForecastHoursTests
{
    private static readonly LaborIntensityCalculator.PpvLine[] NoPpv = [];

    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ComputeEquipmentHours_ByProjectIicGroup_UsesGroupedVolume()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [7] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var lines = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(1, 7, 10m, true),
            new LaborIntensityCalculator.EquipmentLine(2, 7, 5m, true),
            new LaborIntensityCalculator.EquipmentLine(3, 7, 4m, false),
        };

        var hours = _engine.RecalculatePlanHours(
            PlanningScenario.ByProjectIicGroup, NoPpv, lines, norms, z).Equipment;
        var grouped15 = _engine.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 15m, 15m, PlanningZParameters.Default);

        hours[1].Base.Should().Be(Math.Round((grouped15.Base ?? 0m) * (10m / 15m), 2));
        hours[2].Base.Should().Be(Math.Round((grouped15.Base ?? 0m) * (5m / 15m), 2));
        hours[3].Base.Should().BeNull();
        (hours[1].Base + hours[2].Base).Should().BeApproximately(grouped15.Base ?? 0m, 0.02m);
    }

    [Fact]
    public void ComputeEquipmentHours_ByEachValue_StaysPerItem()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [7] = snap };
        var z = new Dictionary<int, PlanningZParameters>();
        var lines = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(1, 7, 10m, true),
            new LaborIntensityCalculator.EquipmentLine(2, 7, 5m, true),
        };

        var hours = _engine.RecalculatePlanHours(
            PlanningScenario.ByEachValue, NoPpv, lines, norms, z).Equipment;
        var expected10 = _engine.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 10m, 10m, PlanningZParameters.Default);
        var expected5 = _engine.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 5m, 5m, PlanningZParameters.Default);

        hours[1].Base.Should().Be(expected10.Base);
        hours[2].Base.Should().Be(expected5.Base);
    }

    private static NormCalculationSnapshot WrightSnap() => new(
        100m, null, null,
        new NormFormula
        {
            Type = FormulaType.Wright,
            MinVolume = 1,
            MaxVolume = 1000,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 0.2m },
        },
        new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0m },
        });
}
