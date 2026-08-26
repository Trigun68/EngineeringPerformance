using EngineeringPerformance.Application.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class ZTrendEngineTests
{
    private readonly ZTrendEngine _engine = new();
    private readonly AnalyticsTrendsOptions _options = new()
    {
        GapThresholdDays = 90,
        LightenStep = 0.18,
        ZClipMax = 5,
        AggregateSameDay = true,
    };

    [Fact]
    public void ClipZ_ClipsAboveMax()
    {
        var clipped = _engine.ClipZ(12m, 5, out var flag);
        clipped.Should().Be(5m);
        flag.Should().Be("clipped");
    }

    [Fact]
    public void ClipZ_Null_ReturnsNoNormFlag()
    {
        var clipped = _engine.ClipZ(null, 5, out var flag);
        clipped.Should().BeNull();
        flag.Should().Be("no_norm");
    }

    [Fact]
    public void WeightedSameDay_ByBasisHours()
    {
        var day = new DateTime(2026, 1, 10);
        var points = new List<ZTrendRawPoint>
        {
            new(day, 1, 1, 2m, 10m, "n1", 1, "N1", 1, "G", "ok"),
            new(day, 2, 2, 0m, 30m, "n1", 1, "N1", 1, "G", "ok"),
        };

        var agg = _engine.AggregateSameDay(points, "basisHours");
        agg.Should().HaveCount(1);
        agg[0].ZScore.Should().Be(0.5m);
        agg[0].ProjectCount.Should().Be(2);
    }

    [Fact]
    public void WeightedSameDay_IgnoresUniform_AlwaysHoursWeighted()
    {
        var day = new DateTime(2026, 1, 10);
        var points = new List<ZTrendRawPoint>
        {
            new(day, 1, 1, 2m, 10m, "n1", 1, "N1", 1, "G", "ok"),
            new(day, 2, 2, 0m, 30m, "n1", 1, "N1", 1, "G", "ok"),
        };

        var agg = _engine.AggregateSameDay(points, "uniform");
        agg.Should().HaveCount(1);
        agg[0].ZScore.Should().Be(0.5m);
    }

    [Fact]
    public void Segment_NormChange_SplitsAndDuplicatesBoundary()
    {
        var points = new List<ZTrendRawPoint>
        {
            new(new DateTime(2025, 1, 1), 1, 1, 1m, 1m, "1", 1, "N1", 1, "G", "ok"),
            new(new DateTime(2025, 2, 1), 2, 1, 0.5m, 1m, "1", 1, "N1", 1, "G", "ok"),
            new(new DateTime(2025, 3, 1), 3, 1, -0.5m, 1m, "2", 2, "N2", 1, "G", "ok"),
            new(new DateTime(2025, 4, 1), 4, 1, 0m, 1m, "2", 2, "N2", 1, "G", "ok"),
        };

        var series = _engine.BuildSeries(points, "Plan", _options);
        series.Segments.Should().HaveCount(2);
        series.Segments[0].Points.Should().HaveCount(2);
        series.Segments[1].Points.Should().HaveCount(3); // boundary duplicate + 2
        series.Segments[1].Points[0].Date.Date.Should().Be(new DateTime(2025, 2, 1));
        series.Segments[0].SegmentKey.Should().Be("1");
        series.Segments[1].SegmentKey.Should().Be("2");
    }

    [Fact]
    public void Segment_LargeGap_NoBoundaryDuplicate()
    {
        var points = new List<ZTrendRawPoint>
        {
            new(new DateTime(2025, 1, 1), 1, 1, 1m, 1m, "1", 1, "N1", 1, "G", "ok"),
            new(new DateTime(2025, 8, 1), 2, 1, 0.5m, 1m, "2", 2, "N2", 1, "G", "ok"),
        };

        var series = _engine.BuildSeries(points, "Fact", _options);
        series.Segments.Should().HaveCount(2);
        series.Segments[1].Points.Should().HaveCount(1);
    }

    [Fact]
    public void Segment_ExcludesNoNormPoints()
    {
        var points = new List<ZTrendRawPoint>
        {
            new(new DateTime(2025, 1, 1), 1, 1, null, 1m, "none", null, "none", 1, "G", "no_norm"),
            new(new DateTime(2025, 2, 1), 2, 1, 1m, 1m, "1", 1, "N1", 1, "G", "ok"),
        };

        var series = _engine.BuildSeries(points, "Plan", _options);
        series.Segments.Should().HaveCount(1);
        series.Segments[0].Points.Should().HaveCount(1);
    }

    [Fact]
    public void ColorForNorm_SecondShade_IsLightened()
    {
        var baseColor = _engine.ColorForSegment(0, 0, 0.18);
        var lighter = _engine.ColorForSegment(0, 1, 0.18);
        baseColor.Should().Be("#1f77b4");
        lighter.Should().NotBe(baseColor);
        lighter.Should().StartWith("#");
    }
}
