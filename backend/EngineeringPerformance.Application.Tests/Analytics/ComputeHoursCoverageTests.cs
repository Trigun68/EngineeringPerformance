using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class ComputeHoursCoverageTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void Coverage_ZShareAndBridgePair_FromEngineNotUi()
    {
        var plan = new[]
        {
            Root(1, "МК", z: null, hours: 100m),
            Leaf(11, 1, volume: 10m, hours: 20m),
        };
        var fact = new[]
        {
            Root(1, "МК", z: 0.5m, hours: 100m),
            Leaf(11, 1, volume: 10m, hours: 25m),
        };

        var coverage = _engine.ComputeHoursCoverage(plan, fact);

        coverage.HoursTotal.Should().Be(100m);
        coverage.HoursWithZ.Should().Be(100m);
        coverage.ShareWithZ.Should().Be(1m);
        coverage.HoursEligibleForBridge.Should().Be(25m);
        coverage.HoursWithBridgePair.Should().Be(25m);
        coverage.ShareWithBridgePair.Should().Be(1m);
    }

    [Fact]
    public void Coverage_MissingPlanVolume_PairShareZero()
    {
        var plan = new[] { Leaf(11, 1, volume: null, hours: 20m) };
        var fact = new[]
        {
            Root(1, "МК", z: null, hours: 40m),
            Leaf(11, 1, volume: 10m, hours: 40m),
        };

        var coverage = _engine.ComputeHoursCoverage(plan, fact);

        coverage.HoursWithZ.Should().Be(0m);
        coverage.ShareWithZ.Should().Be(0m);
        coverage.HoursWithBridgePair.Should().Be(0m);
        coverage.ShareWithBridgePair.Should().Be(0m);
    }

    [Fact]
    public void CombineCoverages_RecalculatesShares()
    {
        var a = _engine.ComputeHoursCoverage(
            [Leaf(11, 1, 10m, 20m)],
            [Root(1, "A", 1m, 50m), Leaf(11, 1, 10m, 20m)]);
        var b = _engine.ComputeHoursCoverage(
            [Leaf(21, 2, 10m, 10m)],
            [Root(2, "B", null, 50m), Leaf(21, 2, 10m, 10m)]);

        var combined = _engine.CombineHoursCoverages([a, b]);

        combined.HoursTotal.Should().Be(100m);
        combined.HoursWithZ.Should().Be(50m);
        combined.ShareWithZ.Should().Be(0.5m);
    }

    [Fact]
    public void RawHoursDelta_FactMinusPlanRoots()
    {
        var plan = new[] { Root(1, "МК", null, 80m) };
        var fact = new[] { Root(1, "МК", 0.2m, 100m) };

        _engine.ComputeRawHoursDelta(plan, fact).Should().Be(20m);
    }

    [Fact]
    public void PercentileBand_NeedsAtLeastTwoWeightedSamples()
    {
        _engine.ComputeWeightedPercentileBand([(1m, 10m)]).Should().BeNull();

        var band = _engine.ComputeWeightedPercentileBand(
        [
            (-1m, 10m),
            (0m, 10m),
            (1m, 10m),
            (2m, 10m),
        ]);

        band.Should().NotBeNull();
        band!.P10.Should().Be(-1m);
        band.P90.Should().Be(2m);
    }

    [Fact]
    public void Movers_SortedByAbsContribution_SumToWeightedZ()
    {
        var rows = _engine.BuildMoverRows(
        [
            new MoverSample(1, "A", "Alpha", 2.0m, 10m),
            new MoverSample(2, "B", "Beta", -1.0m, 10m),
            new MoverSample(3, "C", "Gamma", null, 99m),
        ]);

        rows.Should().HaveCount(2);
        rows[0].ProjectCode.Should().Be("A");
        rows[0].Contribution.Should().Be(1.0m);
        rows[1].Contribution.Should().Be(-0.5m);
        (rows[0].Contribution + rows[1].Contribution).Should().Be(0.5m);
    }

    [Fact]
    public void TopProjectSCurves_OthersAbsorbRemainder()
    {
        var curves = _engine.BuildTopProjectSCurves(
        [
            new ProjectHoursSeriesSample(1, "A", [new HoursAtDateSample(new DateTime(2026, 1, 1), 30m)]),
            new ProjectHoursSeriesSample(2, "B", [new HoursAtDateSample(new DateTime(2026, 1, 1), 20m)]),
            new ProjectHoursSeriesSample(3, "C", [new HoursAtDateSample(new DateTime(2026, 1, 1), 5m)]),
        ], topCount: 2);

        curves.Should().HaveCount(3);
        curves[0].Name.Should().Be("A");
        curves[1].Name.Should().Be("B");
        curves[2].Name.Should().Be("Прочие");
        curves[2].Points[^1].CumulativeHours.Should().Be(5m);
    }

    [Fact]
    public void PlanWaterfall_ComesFromPlanGroups()
    {
        var planPoints = new[]
        {
            new TrendPointSample(new DateTime(2026, 1, 1), 0m, 10m),
            new TrendPointSample(new DateTime(2026, 2, 1), 1m, 10m),
        };
        var planGroups = new[]
        {
            new TrendGroupSample(1, "Группа А", planPoints),
        };

        var detail = _engine.BuildTrendDetailAggregation(planPoints, [], [], planGroups: planGroups);

        detail.PlanKpi.Delta.Should().Be(1m);
        detail.PlanWaterfall.Should().NotBeNull();
        detail.PlanWaterfall!.Should().ContainSingle(s => s.Name == "Группа А");
    }

    private static PerformanceRowDto Root(int id, string name, decimal? z, decimal hours) =>
        new(id, name, $"C{id}", null, 0, "т", null, hours, null, null, null, null, null, z, null, hours);

    private static PerformanceRowDto Leaf(int id, int parentId, decimal? volume, decimal? hours) =>
        new(id, $"L{id}", $"C{id}", parentId, 1, "т", volume, hours, null, null, null, null, null, null, null);
}
