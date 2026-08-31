using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class KimCurveAndAllFormulasTests
{
    private readonly INormCalculator _norm = new NormCalculator();
    private readonly DeviationCalculator _dev = new(new NormCalculator());

    [Fact]
    public void KimCurve_N_IsConstantInVolume()
    {
        var formula = KimNorm(10m);
        _norm.Calculate(formula, 1m).Should().Be(10m);
        _norm.Calculate(formula, 100m).Should().Be(10m);
    }

    [Fact]
    public void KimCurve_Hours_AtZZero_EqualsPTimesV()
    {
        var hours = _dev.CalculateLaborIntensity(KimNorm(10m), KimDev(0.2m), 10m, 10m, 0m);
        hours.Should().Be(100m);
    }

    [Fact]
    public void KimCurve_Hours_AtZHalf_MatchesExpFormula()
    {
        var mu = KimCurveMath.Ln(10m);
        var expected = KimCurveMath.ProductivityAtZ(mu, 0.2m, 0.5m) * 10m;
        var hours = _dev.CalculateLaborIntensity(KimNorm(10m), KimDev(0.2m), 10m, 10m, 0.5m);
        hours.Should().BeApproximately(expected, 0.0001m);
    }

    [Fact]
    public void KimCurve_AtZZero_MatchesConstantHours()
    {
        var kim = _dev.CalculateLaborIntensity(KimNorm(10m), KimDev(0.2m), 10m, 10m, 0m);
        var constant = _dev.CalculateLaborIntensity(
            new NormFormula { Type = FormulaType.Constant, Coefficients = new Dictionary<string, decimal> { ["a"] = 10m } },
            new DeviationFormula { ModelType = DeviationModelType.Multiplicative, Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m } },
            10m, 10m, 0m);
        kim.Should().Be(constant);
    }

    [Fact]
    public void KimCurve_ZScore_WhenFactIsOneSigma_IsOne()
    {
        var p = 10m;
        var mu = KimCurveMath.Ln(p);
        var sigma = 0.2m;
        var factP = KimCurveMath.ProductivityAtZ(mu, sigma, 1m);
        var z = (KimCurveMath.Ln(factP) - mu) / sigma;
        z.Should().BeApproximately(1m, 0.0001m);
        PerformanceCalculationEngine.ProductivityToEstimate(factP).Should().BeApproximately(mu + sigma, 0.0001m);
    }

    [Fact]
    public void KimCurve_Band_IsLognormalNotMultiplicative()
    {
        var band = _dev.GenerateBand(KimNorm(10m), KimDev(0.2m), 10);
        var center = band[0].Center;
        center.Should().BeApproximately(10m, 0.0001m);
        band[0].Upper.Should().BeApproximately(KimCurveMath.ProductivityAtZ(KimCurveMath.Ln(10m), 0.2m, 1m), 0.0001m);
        band[0].Lower.Should().BeApproximately(KimCurveMath.ProductivityAtZ(KimCurveMath.Ln(10m), 0.2m, -1m), 0.0001m);
        band[0].Upper.Should().NotBeApproximately(center * 1.2m, 0.01m);
    }

    [Fact]
    public void ApplyToEntity_Kim_StoresLnPAndLogSigma_NotSigmaTimesP()
    {
        var entity = new IicWorkDirectionNorm { WorkDirectionId = 1 };
        NormDtoMapper.ApplyToEntity(entity, new IicNormWriteRequest(
            WorkDirectionId: 1,
            Performance: 10m,
            Estimate: 99m,
            StandardDeviation: 99m,
            UnitId: null,
            EffectiveFrom: DateTime.UtcNow.Date,
            EffectiveTo: null,
            NormFormula: new NormFormulaDto(
                FormulaType.KimCurve,
                new Dictionary<string, decimal> { ["a"] = 10m, ["sigma"] = 0.2m },
                0, 999_999m, ExtrapolationMode.LinearFromEdge),
            DeviationFormula: new DeviationFormulaDto(
                DeviationModelType.Multiplicative,
                new Dictionary<string, decimal> { ["sigma"] = 0.5m })));

        entity.FormulaType.Should().Be(FormulaType.KimCurve);
        entity.DeviationModelType.Should().Be(DeviationModelType.Lognormal);
        entity.Performance.Should().Be(10m);
        entity.Estimate.Should().BeApproximately(KimCurveMath.Ln(10m), 0.0001m);
        entity.StandardDeviation.Should().Be(0.2m);
        entity.StandardDeviation.Should().NotBe(5m);
    }

    [Fact]
    public void ApplyToEntity_Linear_UnchangedWhenKimExists()
    {
        var entity = new IicWorkDirectionNorm { WorkDirectionId = 1 };
        NormDtoMapper.ApplyToEntity(entity, new IicNormWriteRequest(
            WorkDirectionId: 1,
            Performance: 100m,
            Estimate: 4.605m,
            StandardDeviation: 10m,
            UnitId: null,
            EffectiveFrom: DateTime.UtcNow.Date,
            EffectiveTo: null,
            NormFormula: new NormFormulaDto(
                FormulaType.Linear,
                new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 0m },
                0, 999_999m, ExtrapolationMode.LinearFromEdge),
            DeviationFormula: new DeviationFormulaDto(
                DeviationModelType.Multiplicative,
                new Dictionary<string, decimal> { ["sigma"] = 0.1m })));

        entity.FormulaType.Should().Be(FormulaType.Linear);
        entity.DeviationModelType.Should().Be(DeviationModelType.Multiplicative);
        entity.Performance.Should().Be(100m);
        entity.Estimate.Should().Be(4.605m);
        entity.StandardDeviation.Should().Be(10m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyToEntity_Kim_RejectsNonPositivePerformance(decimal p)
    {
        var entity = new IicWorkDirectionNorm { WorkDirectionId = 1 };
        var act = () => NormDtoMapper.ApplyToEntity(entity, KimRequest(p, 0.2m));
        act.Should().Throw<InvalidOperationException>().WithMessage("*производительность*");
    }

    [Fact]
    public void ApplyToEntity_Kim_RejectsNonPositiveSigma()
    {
        var entity = new IicWorkDirectionNorm { WorkDirectionId = 1 };
        var act = () => NormDtoMapper.ApplyToEntity(entity, KimRequest(10m, 0m));
        act.Should().Throw<InvalidOperationException>().WithMessage("*σ*");
    }

    [Fact]
    public void Linear_Multiplicative_Regression_1050()
    {
        var hours = _dev.CalculateLaborIntensity(
            new NormFormula { Type = FormulaType.Linear, Coefficients = new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 0m } },
            new DeviationFormula { ModelType = DeviationModelType.Multiplicative, Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m } },
            10m, 10m, 0.5m);
        hours.Should().Be(1050m);
    }

    [Theory]
    [MemberData(nameof(AllFormulaTypes))]
    public void AllFormulaTypes_NAndHoursAreNonNegative(FormulaType type)
    {
        var formula = SampleNorm(type);
        var dev = type == FormulaType.KimCurve
            ? KimDev(0.2m)
            : new DeviationFormula
            {
                ModelType = DeviationModelType.Multiplicative,
                Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m },
            };

        foreach (var v in new[] { 1m, 10m, 50m })
        {
            var n = _norm.Calculate(formula, v);
            n.Should().BeGreaterThanOrEqualTo(0m);
            foreach (var z in new[] { -0.5m, 0m, 0.5m })
            {
                var hours = _dev.CalculateLaborIntensity(formula, dev, v, v, z);
                hours.Should().BeGreaterThanOrEqualTo(0m);
            }
        }
    }

    [Fact]
    public void ForecastHours_Kim_RoundsLikeEngine()
    {
        var z = PlanningZParameters.Default;
        var (b, o, p) = ForecastCalculation.ComputeForecastHours(
            KimNorm(10m), KimDev(0.2m), 10m, 10m, z, _dev);
        var mu = KimCurveMath.Ln(10m);
        b.Should().Be(Math.Round(KimCurveMath.ProductivityAtZ(mu, 0.2m, z.ZBase) * 10m, 2));
        o.Should().Be(Math.Round(KimCurveMath.ProductivityAtZ(mu, 0.2m, z.ZOptimistic) * 10m, 2));
        p.Should().Be(Math.Round(KimCurveMath.ProductivityAtZ(mu, 0.2m, z.ZPessimistic) * 10m, 2));
    }

    public static TheoryData<FormulaType> AllFormulaTypes()
    {
        var data = new TheoryData<FormulaType>();
        foreach (var type in Enum.GetValues<FormulaType>())
            data.Add(type);
        return data;
    }

    private static NormFormula KimNorm(decimal p) => new()
    {
        Type = FormulaType.KimCurve,
        Coefficients = new Dictionary<string, decimal>
        {
            ["a"] = p,
            ["mu"] = KimCurveMath.Ln(p),
            ["sigma"] = 0.2m,
        },
    };

    private static DeviationFormula KimDev(decimal sigma) => new()
    {
        ModelType = DeviationModelType.Lognormal,
        Coefficients = new Dictionary<string, decimal> { ["sigma"] = sigma },
    };

    private static IicNormWriteRequest KimRequest(decimal p, decimal sigma) => new(
        WorkDirectionId: 1,
        Performance: p,
        Estimate: null,
        StandardDeviation: sigma,
        UnitId: null,
        EffectiveFrom: DateTime.UtcNow.Date,
        EffectiveTo: null,
        NormFormula: new NormFormulaDto(
            FormulaType.KimCurve,
            new Dictionary<string, decimal> { ["a"] = p, ["sigma"] = sigma },
            0, 999_999m, ExtrapolationMode.LinearFromEdge),
        DeviationFormula: new DeviationFormulaDto(
            DeviationModelType.Lognormal,
            new Dictionary<string, decimal> { ["sigma"] = sigma }));

    private static NormFormula SampleNorm(FormulaType type) => type switch
    {
        FormulaType.Linear => new() { Type = type, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.5m } },
        FormulaType.Logarithmic => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 5 } },
        FormulaType.ShiftedLog => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 5, ["c"] = 1 } },
        FormulaType.Wright => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.2m } },
        FormulaType.Power => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 10, ["b"] = 0.5m } },
        FormulaType.ExponentialDecay => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 100, ["b"] = 0.01m, ["c"] = 5 } },
        FormulaType.PiecewiseLinear => new() { Type = type, PiecewisePoints = [new() { Volume = 0, Norm = 100 }, new() { Volume = 100, Norm = 10 }] },
        FormulaType.Polynomial => new() { Type = type, PolynomialCoeffs = [100m, -1m] },
        FormulaType.Constant => new() { Type = type, Coefficients = new Dictionary<string, decimal> { ["a"] = 12 } },
        FormulaType.Hyperbolic => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 5, ["b"] = 10 } },
        FormulaType.Exponential => new() { Type = type, MinVolume = 1, MaxVolume = 1000, Coefficients = new Dictionary<string, decimal> { ["a"] = 2, ["b"] = 0.01m } },
        FormulaType.KimCurve => KimNorm(10m),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
