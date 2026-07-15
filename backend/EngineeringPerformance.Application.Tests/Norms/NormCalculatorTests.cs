using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class NormCalculatorTests
{
    private readonly INormCalculator _calculator = new NormCalculator();

    [Theory]
    [InlineData(FormulaType.Linear, 100, 10, 90)]
    [InlineData(FormulaType.Linear, 100, 15, 85)]
    public void LinearFormula_NonNegative(FormulaType type, decimal a, decimal v, decimal expected)
    {
        var formula = new NormFormula
        {
            Type = type,
            Coefficients = new Dictionary<string, decimal> { ["a"] = a, ["b"] = 1 },
        };
        _calculator.Calculate(formula, v).Should().Be(expected);
    }

    [Fact]
    public void LinearFormula_WithA0_ReturnsZeroForPositiveVolume()
    {
        var formula = new NormFormula
        {
            Type = FormulaType.Linear,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 0, ["b"] = 5 },
        };
        _calculator.Calculate(formula, 10).Should().Be(0);
    }

    [Fact]
    public void Extrapolation_Zero_ReturnsZeroOutsideBounds()
    {
        var formula = new NormFormula
        {
            Type = FormulaType.Power,
            MinVolume = 10,
            MaxVolume = 100,
            Extrapolation = ExtrapolationMode.Zero,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 2, ["b"] = 1 },
        };
        _calculator.Calculate(formula, 5).Should().Be(0);
        _calculator.Calculate(formula, 200).Should().Be(0);
    }

    [Fact]
    public void Extrapolation_LinearFromEdge_UsesEdgeValue()
    {
        var formula = new NormFormula
        {
            Type = FormulaType.Power,
            MinVolume = 10,
            MaxVolume = 100,
            Extrapolation = ExtrapolationMode.LinearFromEdge,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 2, ["b"] = 1 },
        };
        var atMin = _calculator.Calculate(formula, 10);
        _calculator.Calculate(formula, 5).Should().Be(atMin);
    }

    [Fact]
    public void PiecewiseLinear_Interpolates()
    {
        var formula = new NormFormula
        {
            Type = FormulaType.PiecewiseLinear,
            PiecewisePoints =
            [
                new PiecewisePoint { Volume = 0, Norm = 100 },
                new PiecewisePoint { Volume = 10, Norm = 50 },
                new PiecewisePoint { Volume = 20, Norm = 0 },
            ],
        };
        _calculator.Calculate(formula, 5).Should().Be(75);
        _calculator.Calculate(formula, 20).Should().Be(0);
    }

    [Fact]
    public void Polynomial_DegreeAtMostFive()
    {
        var formula = new NormFormula
        {
            Type = FormulaType.Polynomial,
            PolynomialCoeffs = [10m, -1m, 0.1m],
        };
        _calculator.Calculate(formula, 5).Should().Be(Math.Max(0, 10 - 5 + 0.1m * 25));
    }

    [Theory]
    [InlineData(FormulaType.Logarithmic)]
    [InlineData(FormulaType.ShiftedLog)]
    [InlineData(FormulaType.Wright)]
    [InlineData(FormulaType.Power)]
    [InlineData(FormulaType.ExponentialDecay)]
    public void AllFormulaTypes_CalculateAtThreePoints(FormulaType type)
    {
        var formula = CreateSampleFormula(type);
        foreach (var v in new[] { 1m, 10m, 50m })
        {
            var result = _calculator.Calculate(formula, v);
            result.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    private static NormFormula CreateSampleFormula(FormulaType type) => type switch
    {
        FormulaType.Linear => new NormFormula { Type = type, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.5m } },
        FormulaType.Logarithmic => new NormFormula { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 5 } },
        FormulaType.ShiftedLog => new NormFormula { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 5, ["c"] = 1 } },
        FormulaType.Wright => new NormFormula { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.2m } },
        FormulaType.Power => new NormFormula { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 10, ["b"] = 0.5m } },
        FormulaType.ExponentialDecay => new NormFormula { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.01m, ["c"] = 5 } },
        FormulaType.PiecewiseLinear => new NormFormula { Type = type, PiecewisePoints = [new() { Volume = 0, Norm = 100 }, new() { Volume = 100, Norm = 10 }] },
        FormulaType.Polynomial => new NormFormula { Type = type, PolynomialCoeffs = [100m, -1m] },
        _ => new NormFormula { Type = FormulaType.Linear },
    };
}
