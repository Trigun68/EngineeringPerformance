using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class ComputeHoursBridgeTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void SameVolumeAndHours_BothEffectsZero()
    {
        var plan = new[] { Leaf(11, 1, volume: 10m, hours: 20m) };
        var fact = new[] { Leaf(11, 1, volume: 10m, hours: 20m) };

        var result = _engine.ComputeHoursBridge(plan, fact);

        result.Lines.Should().ContainSingle();
        result.Lines[0].VolumeEffect.Should().Be(0m);
        result.Lines[0].ProductivityEffect.Should().Be(0m);
        result.TotalDelta.Should().Be(0m);
    }

    [Fact]
    public void OnlyVolumeGrew_SameRate_ProductivityZero_VolumeEqualsPlanHours()
    {
        // Ставка плана 2 ч/ед.; объём ×2 → hoursAtPlanRate = 40, volume = +20, prod = 0.
        var plan = new[] { Leaf(11, 1, volume: 10m, hours: 20m) };
        var fact = new[] { Leaf(11, 1, volume: 20m, hours: 40m) };

        var result = _engine.ComputeHoursBridge(plan, fact);

        result.Lines[0].VolumeEffect.Should().Be(20m);
        result.Lines[0].ProductivityEffect.Should().Be(0m);
        result.TotalDelta.Should().Be(20m);
    }

    [Fact]
    public void SameVolume_WorseRate_VolumeZero_ProductivityPositive()
    {
        var plan = new[] { Leaf(11, 1, volume: 10m, hours: 20m) };
        var fact = new[] { Leaf(11, 1, volume: 10m, hours: 30m) };

        var result = _engine.ComputeHoursBridge(plan, fact);

        result.Lines[0].VolumeEffect.Should().Be(0m);
        result.Lines[0].ProductivityEffect.Should().Be(10m);
        result.TotalDelta.Should().Be(10m);
    }

    [Fact]
    public void Identity_VolumePlusProductivity_EqualsDeltaHours()
    {
        var plan = new[] { Leaf(11, 1, volume: 8m, hours: 16m) };
        var fact = new[] { Leaf(11, 1, volume: 12m, hours: 30m) };

        var result = _engine.ComputeHoursBridge(plan, fact);

        var deltaH = 30m - 16m;
        result.TotalDelta.Should().Be(deltaH);
        (result.TotalVolumeEffect + result.TotalProductivityEffect).Should().Be(deltaH);
    }

    [Fact]
    public void LeafRollsUpToRoot_NameTakenFromRoot()
    {
        var plan = new[]
        {
            Root(1, "Металлоконструкции"),
            Leaf(11, 1, volume: 10m, hours: 20m),
        };
        var fact = new[]
        {
            Root(1, "Металлоконструкции"),
            Leaf(11, 1, volume: 20m, hours: 40m),
        };

        var result = _engine.ComputeHoursBridge(plan, fact);

        result.Lines.Should().ContainSingle();
        result.Lines[0].GroupId.Should().Be(1);
        result.Lines[0].GroupName.Should().Be("Металлоконструкции");
        result.Lines[0].VolumeEffect.Should().Be(20m);
    }

    [Fact]
    public void MissingPlanVolume_SkipsGroup()
    {
        var plan = new[] { Leaf(11, 1, volume: null, hours: 20m) };
        var fact = new[] { Leaf(11, 1, volume: 10m, hours: 20m) };

        var result = _engine.ComputeHoursBridge(plan, fact);

        result.Lines.Should().BeEmpty();
        result.TotalDelta.Should().Be(0m);
    }

    [Fact]
    public void UnallocatedAndTotalRows_Ignored()
    {
        var plan = new[]
        {
            new PerformanceRowDto(-1, "Нераспределённые", "", null, 0, null, 5m, 5m, null, null, null, null, null, null, null),
            new PerformanceRowDto(99, "Итого", "", null, 0, null, 10m, 20m, null, null, null, null, null, null, null, IsTotal: true),
        };
        var fact = plan;

        _engine.ComputeHoursBridge(plan, fact).Lines.Should().BeEmpty();
    }

    [Fact]
    public void CombineHoursBridges_SumsByGroupId()
    {
        var a = _engine.ComputeHoursBridge(
            [Leaf(11, 1, 10m, 20m)],
            [Leaf(11, 1, 10m, 30m)]);
        var b = _engine.ComputeHoursBridge(
            [Leaf(11, 1, 10m, 20m)],
            [Leaf(11, 1, 10m, 25m)]);

        var combined = _engine.CombineHoursBridges([a, b]);

        combined.Lines.Should().ContainSingle();
        combined.TotalProductivityEffect.Should().Be(15m);
        combined.TotalVolumeEffect.Should().Be(0m);
    }

    [Fact]
    public void ComputeCumulativeHours_SortsAndAccumulates()
    {
        var points = _engine.ComputeCumulativeHours(
        [
            new HoursAtDateSample(new DateTime(2026, 8, 1), 15m, "v2"),
            new HoursAtDateSample(new DateTime(2026, 5, 1), 10m, "v1"),
        ]);

        points.Should().HaveCount(2);
        points[0].CumulativeHours.Should().Be(10m);
        points[0].Label.Should().Be("v1");
        points[1].CumulativeHours.Should().Be(25m);
    }

    [Fact]
    public void BuildZeroBaselineBars_TopNPlusOthersWeighted()
    {
        var bars = _engine.BuildZeroBaselineBars(
        [
            new ZeroBaselineSample("A", 1.0m, 10m),
            new ZeroBaselineSample("B", -0.5m, 10m),
            new ZeroBaselineSample("C", 0.1m, 10m),
        ], topCount: 2);

        bars.Should().HaveCount(3);
        bars[0].Name.Should().Be("A");
        bars[1].Name.Should().Be("B");
        bars[2].Name.Should().Be("Прочие");
        bars[2].ZScore.Should().Be(0.1m);
    }

    private static PerformanceRowDto Root(int id, string name) =>
        new(id, name, $"C{id}", null, 0, "т", null, null, null, null, null, null, null, null, null);

    private static PerformanceRowDto Leaf(int id, int parentId, decimal? volume, decimal? hours) =>
        new(id, $"L{id}", $"C{id}", parentId, 1, "т", volume, hours, null, null, null, null, null, null, null);
}
