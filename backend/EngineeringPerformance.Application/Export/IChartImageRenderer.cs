namespace EngineeringPerformance.Application.Export;

public interface IChartImageRenderer
{
    byte[] RenderHorizontalBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderVerticalBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderGroupedBarChart(
        string title,
        IReadOnlyList<ChartGroupedBarItem> items,
        string legendA,
        string legendB,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderDeltaBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderMultiLineChart(
        string title,
        IReadOnlyList<ChartLineSeries> series,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderAreaChart(
        string title,
        ChartAreaSeries series,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);

    byte[] RenderScenarioTotalsChart(
        string title,
        double baseHours,
        double optimisticHours,
        double pessimisticHours,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null);
}
