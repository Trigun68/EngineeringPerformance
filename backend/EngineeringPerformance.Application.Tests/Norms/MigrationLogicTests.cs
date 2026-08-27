using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class MigrationLogicTests
{
    [Fact]
    public void Migration_LinearFormula_HasCorrectCoefficients()
    {
        var formula = NormFormulaMapper.ToNormFormula(
            FormulaType.Linear,
            """{"a":150,"b":0}""",
            0, 999999, ExtrapolationMode.LinearFromEdge, 150m);
        formula.Coefficients["a"].Should().Be(150m);
        formula.Coefficients["b"].Should().Be(0m);
    }

    [Fact]
    public void Migration_StdDevConvertedToRatio()
    {
        var dev = NormFormulaMapper.ToDeviationFormula(
            DeviationModelType.Multiplicative,
            """{"sigma":0.1}""",
            100m, 10m);
        dev.Coefficients["sigma"].Should().Be(0.1m);
    }

    [Fact]
    public void Migration_MultiplicativeModelIsDefault()
    {
        var dev = NormFormulaMapper.FromLegacyStdDev(100m, 10m);
        dev.ModelType.Should().Be(DeviationModelType.Multiplicative);
        dev.Coefficients["sigma"].Should().Be(0.1m);
    }

    [Fact]
    public void Migration_CalculationsIdentical_AtZScoreZero()
    {
        const decimal performance = 80m;
        const decimal stdDev = 8m;
        const decimal volume = 10m;
        var z = PlanningZParameters.Default;

        var legacy = ForecastCalculation.ComputeForecastHours(volume, performance, stdDev, z);
        var norm = NormFormulaMapper.FromLegacyPerformance(performance);
        var dev = NormFormulaMapper.FromLegacyStdDev(performance, stdDev);
        var calculator = new DeviationCalculator(new NormCalculator());
        var modern = ForecastCalculation.ComputeForecastHours(norm, dev, volume, volume, z, calculator);

        modern.Base.Should().Be(legacy.Base);
    }

    [Fact]
    public void Migration_HandlesZeroBaseValue()
    {
        var dev = NormFormulaMapper.FromLegacyStdDev(0m, 5m);
        dev.Coefficients["sigma"].Should().Be(0m);
    }

    [Fact]
    public void Migration_NewZScoreBehaviorIsCorrect()
    {
        const decimal performance = 100m;
        const decimal stdDev = 10m;
        const decimal volume = 1m;
        var z = new PlanningZParameters(0.5m, 0m, -0.5m);
        var norm = NormFormulaMapper.FromLegacyPerformance(performance);
        var dev = NormFormulaMapper.FromLegacyStdDev(performance, stdDev);
        var calculator = new DeviationCalculator(new NormCalculator());
        var (_, opt, pes) = ForecastCalculation.ComputeForecastHours(norm, dev, volume, volume, z, calculator);
        pes.Should().Be(105m);
        opt.Should().Be(95m);
    }

    /// <summary>
    /// Z из ProjectVersionPlanningParameters (в т.ч. версии факта) меняет нормативную LI:
    /// LI = N(V)·(1+σ·z)·V — эквивалент Performance·(1 + (StdDev/Performance)·z).
    /// </summary>
    [Fact]
    public void PlanningZBase_AdjustsNormativeLaborIntensity()
    {
        const decimal performance = 100m;
        const decimal stdDev = 10m;
        const decimal volume = 10m;
        var zZero = new PlanningZParameters(0.5m, 0m, -0.5m);
        var zBase = new PlanningZParameters(0.5m, 0.4m, -0.5m);

        var (baseZero, _, _) = ForecastCalculation.ComputeForecastHours(volume, performance, stdDev, zZero);
        var (baseAdj, _, _) = ForecastCalculation.ComputeForecastHours(volume, performance, stdDev, zBase);

        baseZero.Should().Be(1000m);
        baseAdj.Should().Be(1040m);
    }

    [Fact]
    public void Migration_IdempotentCoefficientsJson()
    {
        var formula1 = NormFormulaMapper.ToNormFormula(
            FormulaType.Linear,
            """{"a":150,"b":0}""",
            0, 999999, ExtrapolationMode.LinearFromEdge, 150m);
        var formula2 = NormFormulaMapper.ToNormFormula(
            FormulaType.Linear,
            """{"a":150,"b":0}""",
            0, 999999, ExtrapolationMode.LinearFromEdge, 150m);
        formula1.Coefficients.Should().BeEquivalentTo(formula2.Coefficients);
    }

    [Fact]
    public void Migration_RollbackPreservesLegacyPerformance()
    {
        var performance = 120m;
        var formula = NormFormulaMapper.FromLegacyPerformance(performance);
        formula.Coefficients["a"].Should().Be(performance);
    }

    [Fact]
    public void Migration_DeviationJsonRoundTrip()
    {
        var json = """{"sigma":0.15}""";
        var dev = NormFormulaMapper.ToDeviationFormula(DeviationModelType.Multiplicative, json, 100m, 15m);
        dev.Coefficients["sigma"].Should().Be(0.15m);
    }
}
