using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class ChartRendererTests
{
    private readonly ScottPlotChartRenderer _renderer = new();

    [Fact]
    public void RenderVerticalBarChart_ReturnsPngMagicBytes()
    {
        var bytes = _renderer.RenderVerticalBarChart("Test", new[]
        {
            new ChartBarItem("A", 10),
            new ChartBarItem("B", 20, ReportVisualTheme.AccentA),
        }, 400, 200);

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x89); // PNG
        bytes[1].Should().Be(0x50);
    }

    [Fact]
    public void RenderDeltaBarChart_ReturnsNonEmpty_ForEmptyInput()
    {
        var bytes = _renderer.RenderDeltaBarChart("Empty", Array.Empty<ChartBarItem>());
        bytes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RenderMultiLineChart_ReturnsPng_ForSeries()
    {
        var bytes = _renderer.RenderMultiLineChart("Lines", new[]
        {
            new ChartLineSeries("S1", new[] { (DateTime.UtcNow.AddDays(-1), 100.0), (DateTime.UtcNow, 120.0) }, ReportVisualTheme.AccentA),
        }, 600, 240);
        bytes[0].Should().Be(0x89);
    }
}
