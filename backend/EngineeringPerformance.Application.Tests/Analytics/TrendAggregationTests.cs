using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Помесячная агрегация и водопад детализированных тренд-вкладок.
/// Зеркало фикстур client/src/features/analytics/shared/__tests__/trendDetailCalc.test.ts.
/// </summary>
public sealed class TrendAggregationTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void BucketTrendPointsByMonth_WeightedAverage()
    {
        var points = new[]
        {
            Sample(new DateTime(2026, 5, 10), 1.0m, 50m),
            Sample(new DateTime(2026, 5, 20), -0.5m, 30m),
            Sample(new DateTime(2026, 6, 5), 2.0m, 20m),
        };

        var buckets = _engine.BucketTrendPointsByMonth(points);

        buckets.Should().HaveCount(2);
        buckets[0].Date.Should().Be(new DateTime(2026, 5, 1));
        buckets[0].Value.Should().BeApproximately(0.4375m, 0.0001m);
        buckets[1].Date.Should().Be(new DateTime(2026, 6, 1));
        buckets[1].Value.Should().BeApproximately(2.0m, 0.0001m);
    }

    [Fact]
    public void BucketTrendPointsByMonth_SkipsNullValues()
    {
        var points = new[]
        {
            Sample(new DateTime(2026, 5, 10), null, 50m),
            Sample(new DateTime(2026, 5, 15), 0.5m, 10m),
        };

        var buckets = _engine.BucketTrendPointsByMonth(points);
        buckets.Should().HaveCount(1);
        buckets[0].Value.Should().BeApproximately(0.5m, 0.0001m);
    }

    [Fact]
    public void BucketTrendPointsByMonth_ArithmeticFallbackWhenWeightsZero()
    {
        var points = new[]
        {
            Sample(new DateTime(2026, 5, 10), 1.0m, 0m),
            Sample(new DateTime(2026, 5, 20), 3.0m, 0m),
        };

        var buckets = _engine.BucketTrendPointsByMonth(points);
        buckets[0].Value.Should().BeApproximately(2.0m, 0.0001m);
    }

    [Fact]
    public void BucketTrendPointsByMonth_Empty_ReturnsEmpty()
    {
        _engine.BucketTrendPointsByMonth([]).Should().BeEmpty();
    }

    [Fact]
    public void BucketTrendPointsByMonth_SortsChronologically()
    {
        var points = new[]
        {
            Sample(new DateTime(2026, 8, 1), 3m, 1m),
            Sample(new DateTime(2026, 5, 1), 1m, 1m),
            Sample(new DateTime(2026, 6, 1), 2m, 1m),
        };

        var dates = _engine.BucketTrendPointsByMonth(points).Select(b => b.Date).ToList();
        dates.Should().Equal(
            new DateTime(2026, 5, 1),
            new DateTime(2026, 6, 1),
            new DateTime(2026, 8, 1));
    }

    [Fact]
    public void ComputePeriodKpi_FirstLastAndDelta()
    {
        var buckets = _engine.BucketTrendPointsByMonth(
        [
            Sample(new DateTime(2026, 5, 1), -0.42m, 10m),
            Sample(new DateTime(2026, 6, 1), -0.31m, 10m),
            Sample(new DateTime(2026, 8, 1), 0.05m, 10m),
        ]);

        var kpi = _engine.ComputePeriodKpi(buckets);
        kpi.Initial.Should().BeApproximately(-0.42m, 0.0001m);
        kpi.Current.Should().BeApproximately(0.05m, 0.0001m);
        kpi.Delta.Should().BeApproximately(0.47m, 0.0001m);
    }

    [Fact]
    public void ComputePeriodKpi_Empty_ReturnsNulls()
    {
        var empty = _engine.ComputePeriodKpi([]);
        empty.Should().Be(new PeriodKpiDto(null, null, null));
        _engine.ComputePeriodKpi([new MonthlyTrendBucketDto(new DateTime(2026, 5, 1), null)])
            .Should().Be(new PeriodKpiDto(null, null, null));
    }

    [Fact]
    public void BuildFactorWaterfall_WeightsGroupDeltasByHourShare()
    {
        var steps = _engine.BuildFactorWaterfall(TwoGroups(), 0.2m);

        steps.Should().NotBeNull();
        steps!.Should().HaveCount(2);
        steps[0].Name.Should().Be("Металлоконструкции");
        steps[0].Value.Should().BeApproximately(0.3m, 0.0001m);
        steps[1].Name.Should().Be("Трубопроводы");
        steps[1].Value.Should().BeApproximately(-0.1m, 0.0001m);
        steps.Sum(s => s.Value).Should().BeApproximately(0.2m, 0.0001m);
    }

    [Fact]
    public void BuildFactorWaterfall_AddsOthersAbsorbingResidual()
    {
        var many = Enumerable.Range(1, 5)
            .Select(i => new TrendGroupSample(
                i,
                $"Направление {i}",
                [
                    Sample(new DateTime(2026, 5, 1), 0m, 10m),
                    Sample(new DateTime(2026, 8, 1), 0.1m * i, 10m),
                ]))
            .ToList();

        var steps = _engine.BuildFactorWaterfall(many, 0.3m, 3);

        steps.Should().HaveCount(4);
        steps![3].Name.Should().Be("Прочие направления");
        steps[3].Value.Should().BeApproximately(0.06m, 0.0001m);
        steps.Sum(s => s.Value).Should().BeApproximately(0.3m, 0.0001m);
    }

    [Fact]
    public void BuildFactorWaterfall_NullWhenDeltaOrWeightsMissing()
    {
        _engine.BuildFactorWaterfall(TwoGroups(), null).Should().BeNull();
        _engine.BuildFactorWaterfall([], 0.2m).Should().BeNull();
        var zeroWeight = new[]
        {
            new TrendGroupSample(1, "A", [Sample(new DateTime(2026, 5, 1), 1m, 0m)]),
        };
        _engine.BuildFactorWaterfall(zeroWeight, 0.2m).Should().BeNull();
    }

    private static IReadOnlyList<TrendGroupSample> TwoGroups() =>
    [
        new TrendGroupSample(
            1,
            "Металлоконструкции",
            [
                Sample(new DateTime(2026, 5, 1), 0.0m, 60m),
                Sample(new DateTime(2026, 8, 1), 0.5m, 60m),
            ]),
        new TrendGroupSample(
            2,
            "Трубопроводы",
            [
                Sample(new DateTime(2026, 5, 1), 0.0m, 40m),
                Sample(new DateTime(2026, 8, 1), -0.25m, 40m),
            ]),
    ];

    private static TrendPointSample Sample(DateTime date, decimal? value, decimal weight) =>
        new(date, value, weight);
}
