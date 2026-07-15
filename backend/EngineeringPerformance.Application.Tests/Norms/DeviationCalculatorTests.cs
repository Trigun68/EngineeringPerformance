using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class DeviationCalculatorTests
{
    private readonly DeviationCalculator _calculator = new(new NormCalculator());

    private static NormFormula FlatNorm(decimal a) => new()
    {
        Type = FormulaType.Linear,
        Coefficients = new Dictionary<string, decimal> { ["a"] = a, ["b"] = 0 },
    };

    [Fact]
    public void MultiplicativeModel_LI_Correct()
    {
        var dev = new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m },
        };
        var li = _calculator.CalculateLaborIntensity(FlatNorm(100), dev, 10, 10, 0.5m);
        li.Should().Be(1050m);
    }

    [Fact]
    public void GenerateBand_Multiplicative_UpperLowerCorrect()
    {
        var dev = new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m },
        };
        var band = _calculator.GenerateBand(FlatNorm(100), dev, 10);
        band[5].Upper.Should().BeApproximately(110m, 0.01m);
        band[5].Lower.Should().BeApproximately(90m, 0.01m);
    }

    [Fact]
    public void GenerateBand_Constant_UpperLowerCorrect()
    {
        var dev = new DeviationFormula
        {
            ModelType = DeviationModelType.Constant,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 5m },
        };
        var band = _calculator.GenerateBand(FlatNorm(100), dev, 10);
        band[3].Upper.Should().Be(105m);
        band[3].Lower.Should().Be(95m);
    }

    [Fact]
    public void GenerateBand_LowerClampedAtZero()
    {
        var dev = new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 2m },
        };
        var band = _calculator.GenerateBand(FlatNorm(10), dev, 10);
        band.Should().OnlyContain(p => p.Lower >= 0);
    }

    [Fact]
    public void GenerateBand_PointsCount()
    {
        var dev = new DeviationFormula { ModelType = DeviationModelType.Multiplicative, Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m } };
        _calculator.GenerateBand(FlatNorm(100), dev, 50).Should().HaveCount(50);
    }

    [Fact]
    public void GenerateBand_VolumeRangeRespected()
    {
        var norm = new NormFormula
        {
            Type = FormulaType.Power,
            MinVolume = 5,
            MaxVolume = 50,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 10, ["b"] = 1 },
        };
        var dev = new DeviationFormula { ModelType = DeviationModelType.Multiplicative, Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.05m } };
        var band = _calculator.GenerateBand(norm, dev, 20);
        band.First().Volume.Should().BeApproximately(5m, 0.01m);
        band.Last().Volume.Should().BeApproximately(50m, 0.01m);
    }
}
