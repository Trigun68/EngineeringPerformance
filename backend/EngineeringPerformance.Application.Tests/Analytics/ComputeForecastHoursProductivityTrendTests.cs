using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Индекс и декомпозиция тренда производительности считаются из уже существующего
/// <see cref="IPerformanceCalculationEngine.ComputeForecastHours"/> (ZBase = 0 → Base ≈ N(V)·V).
/// Новых методов движка нет.
/// </summary>
public sealed class ComputeForecastHoursProductivityTrendTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ComputeForecastHours_LinearVsLog_AtSameV_DifferenceIsStandardEffect()
    {
        const decimal vStar = 10m;
        var linear = Formula(FormulaType.Linear, a: 10m, b: 0.1m, minVolume: 0m);
        var log = Formula(FormulaType.Logarithmic, a: 10m, b: 0.5m, minVolume: 1m);

        var hoursOld = _engine.ComputeForecastHours(linear, Dev(), vStar, vStar, PlanningZParameters.Default);
        var hoursNew = _engine.ComputeForecastHours(log, Dev(), vStar, vStar, PlanningZParameters.Default);

        hoursOld.Base.Should().HaveValue();
        hoursNew.Base.Should().HaveValue();
        hoursOld.Base.Should().NotBe(hoursNew.Base);

        var standardEffect = hoursNew.Base!.Value - hoursOld.Base!.Value;
        var mixSameV = hoursNew.Base.Value - hoursNew.Base.Value;
        mixSameV.Should().Be(0m);
        standardEffect.Should().NotBe(0m);
    }

    [Fact]
    public void ComputeForecastHours_SameFormula_DifferentV_DifferenceIsMixEffect()
    {
        const decimal vOld = 10m;
        const decimal vNew = 20m;
        var log = Formula(FormulaType.Logarithmic, a: 10m, b: 0.5m, minVolume: 1m);

        var hoursNewAtOldV = _engine.ComputeForecastHours(log, Dev(), vOld, vOld, PlanningZParameters.Default);
        var hoursNewAtNewV = _engine.ComputeForecastHours(log, Dev(), vNew, vNew, PlanningZParameters.Default);

        var mix = hoursNewAtNewV.Base!.Value - hoursNewAtOldV.Base!.Value;
        mix.Should().NotBe(0m);
    }

    [Fact]
    public void ComputeForecastHours_Logarithmic_VolumeBelowOne_ReturnsZero()
    {
        var log = Formula(FormulaType.Logarithmic, a: 10m, b: 1m, minVolume: 0m);
        var hours = _engine.ComputeForecastHours(log, Dev(), 0.5m, 0.5m, PlanningZParameters.Default);
        hours.Base.Should().Be(0m);
    }

    [Fact]
    public void ComputeForecastHours_DefaultZ_BaseEqualsHoursAtReferenceVolume()
    {
        const decimal vStar = 10m;
        var linear = Formula(FormulaType.Linear, a: 10m, b: 0.1m, minVolume: 0m);
        var hours = _engine.ComputeForecastHours(linear, Dev(), vStar, vStar, PlanningZParameters.Default);
        hours.Base.Should().Be(90m);
        var nPerUnit = hours.Base!.Value / vStar;
        nPerUnit.Should().Be(9m);
    }

    private static NormFormula Formula(FormulaType type, decimal a, decimal b, decimal minVolume) =>
        new()
        {
            Type = type,
            MinVolume = minVolume,
            MaxVolume = 1000m,
            Coefficients = new Dictionary<string, decimal> { ["a"] = a, ["b"] = b },
        };

    private static DeviationFormula Dev() =>
        new()
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0m },
        };
}
