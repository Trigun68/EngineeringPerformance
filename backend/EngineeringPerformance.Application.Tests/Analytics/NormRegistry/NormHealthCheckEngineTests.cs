using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics.NormRegistry;

public sealed class NormHealthCheckEngineTests
{
    private static readonly DateTime Today = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly NormHealthOptions DefaultOptions = new();

    [Fact]
    public void Detect_OverlappingActiveNorms()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 10, "Электрика", from: new DateTime(2025, 1, 1), to: new DateTime(2026, 6, 1)),
            Norm(2, 10, "Электрика", from: new DateTime(2026, 5, 1), to: null),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "OVERLAP_CONFLICT");
        summary.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void NoDetect_AdjacentPeriods()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 10, "G", from: new DateTime(2025, 1, 1), to: new DateTime(2026, 1, 1)),
            Norm(2, 10, "G", from: new DateTime(2026, 1, 2), to: null),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().NotContain(i => i.RuleCode == "OVERLAP_CONFLICT");
    }

    [Fact]
    public void Detect_Overlap_WithNullEffectiveTo()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 10, "G", from: new DateTime(2024, 1, 1), to: null),
            Norm(2, 10, "G", from: new DateTime(2025, 1, 1), to: new DateTime(2025, 6, 1)),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "OVERLAP_CONFLICT");
        summary.Issues.Should().Contain(i => i.RuleCode == "STALE_ACTIVE");
    }

    [Fact]
    public void Detect_GapBetweenNorms()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 20, "Трубопроводы", from: new DateTime(2024, 1, 1), to: new DateTime(2025, 1, 1)),
            Norm(2, 20, "Трубопроводы", from: new DateTime(2025, 6, 1), to: null),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "COVERAGE_GAP");
    }

    [Fact]
    public void Detect_GroupWithoutActiveNormOnToday()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 30, "Пустая", from: new DateTime(2020, 1, 1), to: new DateTime(2021, 1, 1)),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "NO_ACTIVE_NORM");
    }

    [Fact]
    public void Detect_ZeroSigma()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 40, "G", from: new DateTime(2025, 1, 1), to: null, sigma: 0m),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "ZERO_SIGMA");
    }

    [Fact]
    public void Detect_NegativeBaseForPower()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 40, "G", from: new DateTime(2025, 1, 1), to: null,
                type: FormulaType.Power,
                coeffs: new Dictionary<string, decimal> { ["a"] = -1m, ["b"] = 2m },
                sigma: 1m),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "INVALID_PARAMS");
    }

    [Fact]
    public void Detect_SimilarFormulasAndParams()
    {
        var coeffs = new Dictionary<string, decimal> { ["a"] = 2m, ["b"] = 3m };
        var coeffs2 = new Dictionary<string, decimal> { ["a"] = 2.05m, ["b"] = 3.01m };
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 50, "G", from: new DateTime(2025, 1, 1), to: new DateTime(2026, 12, 1), coeffs: coeffs, sigma: 1m),
            Norm(2, 50, "G", from: new DateTime(2026, 6, 1), to: null, coeffs: coeffs2, sigma: 1m),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "DUPLICATE_NORM");
    }

    [Fact]
    public void Detect_ActiveNormNoVersionsInWindow()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 60, "G", from: new DateTime(2025, 1, 1), to: null, sigma: 1m),
        };
        var usage = new Dictionary<int, NormUsageHint>
        {
            [1] = new(1, 0, Today.AddDays(-200)),
        };

        var summary = NormHealthCheckEngine.Run(norms, usage, DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "UNUSED_NORM");
    }

    [Fact]
    public void Detect_StaleFuture()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 70, "G",
                from: Today.AddDays(30),
                to: null,
                sigma: 1m,
                createdAt: Today.AddDays(-100)),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "STALE_FUTURE");
    }

    [Fact]
    public void Detect_ChainBreakBeyondTolerance()
    {
        var norms = new List<NormHealthNormInput>
        {
            Norm(1, 80, "G", from: new DateTime(2024, 1, 1), to: new DateTime(2025, 1, 1), sigma: 1m),
            Norm(2, 80, "G", from: new DateTime(2025, 1, 10), to: null, sigma: 1m),
        };

        var summary = NormHealthCheckEngine.Run(norms, EmptyUsage(), DefaultOptions, Today);

        summary.Issues.Should().Contain(i => i.RuleCode == "BROKEN_CHAIN");
    }

    [Fact]
    public void EmptyBase_ReturnsZeroIssues()
    {
        var summary = NormHealthCheckEngine.Run([], EmptyUsage(), DefaultOptions, Today);
        summary.Issues.Should().BeEmpty();
        summary.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void DerivedStatus_ResolvesCorrectly()
    {
        NormDerivedStatus.Resolve(Today.AddDays(-10), null, Today).Should().Be("Active");
        NormDerivedStatus.Resolve(Today.AddDays(-100), Today.AddDays(-1), Today).Should().Be("Historical");
        NormDerivedStatus.Resolve(Today.AddDays(5), null, Today).Should().Be("Future");
    }

    private static IReadOnlyDictionary<int, NormUsageHint> EmptyUsage() =>
        new Dictionary<int, NormUsageHint>();

    private static NormHealthNormInput Norm(
        int id,
        int groupId,
        string groupName,
        DateTime from,
        DateTime? to,
        FormulaType type = FormulaType.Linear,
        IReadOnlyDictionary<string, decimal>? coeffs = null,
        decimal? sigma = 1m,
        DateTime? createdAt = null) =>
        new(
            Id: id,
            WorkDirectionId: groupId,
            GroupName: groupName,
            GroupCode: $"G{groupId}",
            EffectiveFrom: from,
            EffectiveTo: to,
            FormulaType: type,
            Coefficients: coeffs ?? new Dictionary<string, decimal> { ["a"] = 1m, ["b"] = 2m },
            StandardDeviation: sigma,
            DeviationModelType: DeviationModelType.Multiplicative,
            DeviationCoefficients: new Dictionary<string, decimal>(),
            CreatedAt: createdAt ?? from);
}
