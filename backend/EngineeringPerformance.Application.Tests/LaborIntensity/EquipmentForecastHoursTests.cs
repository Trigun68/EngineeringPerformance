using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

public sealed class EquipmentForecastHoursTests
{
    private static readonly DeviationCalculator Calculator = new(new NormCalculator());

    [Fact]
    public void ComputeEquipmentHours_ByProjectIicGroup_UsesGroupedVolume()
    {
        var snap = WrightSnap();
        var calculator = Calculator;
        var norms = new Dictionary<int, NormCalculationSnapshot> { [7] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var lines = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(1, 7, 10m, true),
            new LaborIntensityCalculator.EquipmentLine(2, 7, 5m, true),
            new LaborIntensityCalculator.EquipmentLine(3, 7, 4m, false),
        };

        var hours = ForecastHoursAggregator.ComputeEquipmentHours(
            PlanningScenario.ByProjectIicGroup, lines, [], norms, z, calculator);
        var grouped15 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 15m, 15m, PlanningZParameters.Default, calculator);

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

        var hours = ForecastHoursAggregator.ComputeEquipmentHours(
            PlanningScenario.ByEachValue, lines, [], norms, z, Calculator);
        var expected10 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 10m, 10m, PlanningZParameters.Default, Calculator);
        var expected5 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 5m, 5m, PlanningZParameters.Default, Calculator);

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
