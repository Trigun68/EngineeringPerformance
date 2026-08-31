using EngineeringPerformance.Application.Analytics.FactNorm;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics.FactNorm;

public sealed class ReferenceFormulaTests
{
    [Fact]
    public void Linear_Fit_RecoversKnownParams()
    {
        var rng = new Random(42);
        var points = Enumerable.Range(1, 40)
            .Select(i =>
            {
                var x = i;
                var y = 2 + 3 * x + rng.NextDouble() * 0.01;
                return new FactPoint(x, y);
            })
            .ToList();

        var fit = new LinearReferenceFormula().Fit(points);
        fit.Success.Should().BeTrue();
        fit.Parameters["a"].Should().BeApproximately(2, 0.05);
        fit.Parameters["b"].Should().BeApproximately(3, 0.05);
        fit.Metrics.R2.Should().BeGreaterThan(0.99);
    }

    [Fact]
    public void Power_Fit_RecoversLearningCurve()
    {
        var points = Enumerable.Range(1, 50)
            .Select(i => new FactPoint(i, 14 * Math.Pow(i, -0.18)))
            .ToList();

        var fit = new PowerReferenceFormula().Fit(points);
        fit.Success.Should().BeTrue();
        fit.Parameters["a"].Should().BeApproximately(14, 0.5);
        fit.Parameters["b"].Should().BeApproximately(-0.18, 0.02);
    }

    [Fact]
    public void Fit_WithOnePoint_ReturnsInsufficientViaCanFit()
    {
        var formula = new LinearReferenceFormula();
        formula.CanFit([new FactPoint(1, 2)], minPoints: 5).Should().BeFalse();
    }

    [Fact]
    public void CanFit_FalseWhenTooFewPoints()
    {
        var points = Enumerable.Range(1, 3).Select(i => new FactPoint(i, i)).ToList();
        new Polynomial2ReferenceFormula().CanFit(points, 5).Should().BeFalse();
    }

    [Fact]
    public void Fit_ReturnsValidR2()
    {
        var points = Enumerable.Range(1, 20).Select(i => new FactPoint(i, 5 + 0.5 * i)).ToList();
        var fit = new LinearReferenceFormula().Fit(points);
        fit.Metrics.R2.Should().BeInRange(0, 1);
    }

    [Fact]
    public void Hyperbolic_Fit_RecoversParams()
    {
        var points = Enumerable.Range(1, 30)
            .Select(i => new FactPoint(i, 2 + 10.0 / i))
            .ToList();
        var fit = new HyperbolicReferenceFormula().Fit(points);
        fit.Success.Should().BeTrue();
        fit.Parameters["a"].Should().BeApproximately(2, 0.1);
        fit.Parameters["b"].Should().BeApproximately(10, 0.5);
    }

    [Fact]
    public void Constant_Fit_UsesMean()
    {
        var points = new[] { new FactPoint(1, 10), new FactPoint(2, 10), new FactPoint(3, 10) };
        var fit = new ConstantReferenceFormula().Fit(points);
        fit.Parameters["a"].Should().BeApproximately(10, 1e-9);
        fit.Metrics.R2.Should().BeApproximately(1, 1e-9);
    }
}

public sealed class ModelSelectionTests
{
    private static AnalyticsOptions Options() => new();

    [Fact]
    public void Select_BetterPowerOverLinear_WhenDataIsLearningCurve()
    {
        var points = Enumerable.Range(1, 60)
            .Select(i => new FactPoint(i, 14 * Math.Pow(i, -0.18)))
            .ToList();

        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            NormId: 1,
            WorkDirectionId: 10,
            GroupName: "Трубопроводы",
            CurrentNorm: new CurrentNormFitContext(
                ReferenceFormulaCodes.Linear,
                new Dictionary<string, double> { ["a"] = 2, ["b"] = 3 },
                MeanPerformance: 10,
                StdDeviation: 1,
                CurrentR2: 0.2),
            Points: points);

        var result = engine.Run(new DateTime(2025, 1, 1), new DateTime(2026, 2, 1), [input], Options());
        var row = result.Comparisons.Should().ContainSingle().Subject;
        row.Verdict.Should().Be(NormVerdict.ChangeFormulaType);
        row.ProposedFormula.Should().Be(ReferenceFormulaCodes.Power);
        row.Suggestion.Should().NotBeNull();
    }

    [Fact]
    public void Select_Linear_WhenDataIsLinear_WithDrift_AdjustParameters()
    {
        var points = Enumerable.Range(1, 60)
            .Select(i => new FactPoint(i, 7 + 1.0 * i))
            .ToList();

        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            NormId: 2,
            WorkDirectionId: 20,
            GroupName: "Оборудование",
            CurrentNorm: new CurrentNormFitContext(
                ReferenceFormulaCodes.Linear,
                new Dictionary<string, double> { ["a"] = 5, ["b"] = 1 },
                MeanPerformance: 12,
                StdDeviation: 2,
                CurrentR2: 0.9),
            Points: points);

        var result = engine.Run(new DateTime(2025, 1, 1), new DateTime(2026, 2, 1), [input], Options());
        var row = result.Comparisons.Single();
        row.Verdict.Should().Be(NormVerdict.AdjustParameters);
        row.ProposedFormula.Should().Be(ReferenceFormulaCodes.Linear);
    }

    [Fact]
    public void Engine_EmptyPoints_InsufficientData()
    {
        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            1, 1, "G",
            new CurrentNormFitContext(ReferenceFormulaCodes.Linear, new Dictionary<string, double> { ["a"] = 1, ["b"] = 0 }, 1, 1, 0.5),
            Array.Empty<FactPoint>());
        var result = engine.Run(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, [input], Options());
        result.Comparisons.Single().Verdict.Should().Be(NormVerdict.InsufficientData);
    }

    [Fact]
    public void Engine_ComputesAccuracyFromPeriod()
    {
        var points = Enumerable.Range(1, 60).Select(i => new FactPoint(i, 5 + i)).ToList();
        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            1, 1, "G",
            new CurrentNormFitContext(ReferenceFormulaCodes.Linear, new Dictionary<string, double> { ["a"] = 5, ["b"] = 1 }, 10, 1, 0.99),
            points);
        var result = engine.Run(new DateTime(2024, 1, 1), new DateTime(2025, 6, 1), [input], Options());
        result.Accuracy.Level.Should().Be(AccuracyLevel.High);
    }
}

public sealed class AccuracyLevelTests
{
    [Fact]
    public void PeriodUnder180Days_ReturnsLow()
    {
        var r = AccuracyLevelCalculator.Compute(
            DateTime.Today.AddDays(-90), DateTime.Today, 100, new AccuracyOptions());
        r.Level.Should().Be(AccuracyLevel.Low);
        r.Hint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PeriodOver365With50Points_ReturnsHigh()
    {
        var r = AccuracyLevelCalculator.Compute(
            new DateTime(2024, 1, 1), new DateTime(2025, 2, 1), 60, new AccuracyOptions());
        r.Level.Should().Be(AccuracyLevel.High);
    }
}

public sealed class KimCurveFactNormTests
{
    [Fact]
    public void Catalog_All_DoesNotIncludeKimCurve()
    {
        ReferenceFormulaCatalog.All.Should().HaveCount(7);
        ReferenceFormulaCatalog.All.Should().NotContain(f => f.Code == ReferenceFormulaCodes.KimCurve);
        ReferenceFormulaCodes.All.Should().NotContain(ReferenceFormulaCodes.KimCurve);
        ReferenceFormulaCatalog.TryGet(ReferenceFormulaCodes.KimCurve).Should().NotBeNull();
        ReferenceFormulaCatalog.TryGet(ReferenceFormulaCodes.KimCurve)!.DisplayName.Should().Be("Кривая Кима");
    }

    [Fact]
    public void FormulaTypeMapper_RoundTripsKimCurve()
    {
        FormulaTypeMapper.ToReferenceCode(EngineeringPerformance.Domain.Norms.FormulaType.KimCurve)
            .Should().Be(ReferenceFormulaCodes.KimCurve);
        FormulaTypeMapper.ToFormulaType(ReferenceFormulaCodes.KimCurve)
            .Should().Be(EngineeringPerformance.Domain.Norms.FormulaType.KimCurve);
    }

    [Fact]
    public void Engine_KimCurrentNorm_UsesLogSpaceSigma_AndDoesNotProposeKim()
    {
        const double p = 10.0;
        const double mu = 2.302585092994046; // ln(10)
        const double sigma = 0.2;
        var points = Enumerable.Range(0, 60)
            .Select(i =>
            {
                var z = (i % 3) - 1; // -1, 0, 1
                return new FactPoint(i + 1, Math.Exp(mu + sigma * z));
            })
            .ToList();

        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            1, 1, "Kim",
            new CurrentNormFitContext(
                ReferenceFormulaCodes.KimCurve,
                new Dictionary<string, double> { ["a"] = p },
                p,
                sigma,
                null),
            points);
        var result = engine.Run(new DateTime(2025, 1, 1), new DateTime(2026, 8, 1), [input], new AnalyticsOptions());
        var row = result.Comparisons.Should().ContainSingle().Subject;
        row.FactStdDeviation.Should().BeApproximately(Math.Sqrt(2.0 / 3.0) * sigma, 0.01);
        if (row.Suggestion is not null)
            row.Suggestion.ProposedFormulaType.Should().NotBe(ReferenceFormulaCodes.KimCurve);
    }

    [Fact]
    public void Engine_KimCurrentNorm_FlatFacts_HaveR2NearOne()
    {
        var points = Enumerable.Range(1, 40).Select(i => new FactPoint(i, 10)).ToList();
        var engine = new FactNormAnalysisEngine();
        var input = new NormGroupComparisonInput(
            1, 1, "Kim",
            new CurrentNormFitContext(
                ReferenceFormulaCodes.KimCurve,
                new Dictionary<string, double> { ["a"] = 10 },
                10,
                0.2,
                null),
            points);
        var result = engine.Run(new DateTime(2025, 1, 1), new DateTime(2026, 8, 1), [input], new AnalyticsOptions());
        result.Comparisons.Single().CurrentR2.Should().BeApproximately(1, 1e-9);
    }
}
