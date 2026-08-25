using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

public sealed class EquipmentForecastHoursTests
{
    [Fact]
    public void ComputeEquipmentHours_PerItem_NotGroupedByScenario()
    {
        var snap = new NormCalculationSnapshot(
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
        var calculator = new DeviationCalculator(new NormCalculator());
        var norms = new Dictionary<int, NormCalculationSnapshot> { [7] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var lines = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(1, 7, 10m, true),
            new LaborIntensityCalculator.EquipmentLine(2, 7, 5m, true),
            new LaborIntensityCalculator.EquipmentLine(3, 7, 4m, false),
        };

        var hours = ForecastHoursAggregator.ComputeEquipmentHours(lines, norms, z, calculator);
        var expected10 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 10m, 10m, PlanningZParameters.Default, calculator);
        var expected5 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 5m, 5m, PlanningZParameters.Default, calculator);
        var grouped15 = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, 15m, 15m, PlanningZParameters.Default, calculator);

        hours[1].Base.Should().Be(expected10.Base);
        hours[2].Base.Should().Be(expected5.Base);
        hours[3].Base.Should().BeNull();
        (hours[1].Base + hours[2].Base).Should().NotBe(grouped15.Base);
    }
}
