using EngineeringPerformance.Application.Export;
using ScottPlot;
using ScottPlot.Plottables;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Рендер графиков для PDF через ScottPlot → PNG. Заголовок графика не рисуется внутри PNG —
/// он выводится отдельным H2-блоком в QuestPDF (<see cref="QuestPdfReportComposer.ComposeChartImage"/>),
/// здесь остаются только оси, данные и подписи — единая палитра «Executive Technical».</summary>
public sealed class ScottPlotChartRenderer : IChartImageRenderer
{
    private static bool _fontInitialized;

    private static void EnsureCyrillicFont()
    {
        if (_fontInitialized)
            return;

        _fontInitialized = true;
        var path = Path.Combine(AppContext.BaseDirectory, ReportVisualTheme.EmbeddedFontRelativePath);
        if (File.Exists(path))
        {
            Fonts.AddFontFile(ReportVisualTheme.EmbeddedFontName, path);
            Fonts.Default = ReportVisualTheme.EmbeddedFontName;
        }
    }

    private static ChartRenderOptions Opt(ChartRenderOptions? options) => options ?? new ChartRenderOptions();

    private static Plot CreatePlot(string title, IEnumerable<string> sampleTexts, ChartRenderOptions? options)
    {
        EnsureCyrillicFont();
        var opts = Opt(options);

        var plot = new Plot();
        // Заголовок не рисуется в PNG — дублируется как H2 в PDF-слое (см. ComposeChartImage).
        plot.Title(string.Empty);
        plot.Axes.Title.Label.IsVisible = false;
        plot.FigureBackground.Color = Color.FromHex("#FFFFFF");
        plot.DataBackground.Color = Color.FromHex("#FBFCFD");

        // Единая сетка «Executive Technical»: только горизонтальные линии, тонкие, приглушённые.
        plot.Grid.IsVisible = opts.ShowGrid;
        plot.Grid.MajorLineColor = Color.FromHex(ReportVisualTheme.RuleColor);
        plot.Grid.MajorLineWidth = 1;
        plot.Grid.XAxisStyle.IsVisible = false;
        plot.Grid.YAxisStyle.IsVisible = opts.ShowGrid;

        var fontName = ResolveFont(sampleTexts);
        ApplyFont(plot, fontName);

        plot.Axes.Bottom.TickLabelStyle.FontSize = 9;
        plot.Axes.Left.TickLabelStyle.FontSize = 9;
        plot.Axes.Bottom.Label.FontSize = 10.5f;
        plot.Axes.Left.Label.FontSize = 10.5f;
        plot.Axes.Bottom.TickLabelStyle.ForeColor = Color.FromHex(ReportVisualTheme.Primary);
        plot.Axes.Left.TickLabelStyle.ForeColor = Color.FromHex(ReportVisualTheme.Primary);
        plot.Axes.Bottom.Label.ForeColor = Color.FromHex(ReportVisualTheme.Muted);
        plot.Axes.Left.Label.ForeColor = Color.FromHex(ReportVisualTheme.Muted);
        plot.Axes.Color(Color.FromHex(ReportVisualTheme.RuleColor));

        if (!string.IsNullOrWhiteSpace(opts.XAxisLabel))
        {
            plot.Axes.Bottom.Label.Text = opts.XAxisLabel;
            plot.Axes.Bottom.Label.FontName = fontName;
        }

        if (!string.IsNullOrWhiteSpace(opts.YAxisLabel))
        {
            plot.Axes.Left.Label.Text = opts.YAxisLabel;
            plot.Axes.Left.Label.FontName = fontName;
        }

        return plot;
    }

    private static string ResolveFont(IEnumerable<string> sampleTexts)
    {
        var sample = string.Join(" ", sampleTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (string.IsNullOrWhiteSpace(sample))
            return Fonts.Default;

        var detected = Fonts.Detect(sample);
        return string.IsNullOrWhiteSpace(detected) ? Fonts.Default : detected;
    }

    private static void ApplyFont(Plot plot, string fontName)
    {
        plot.Axes.Bottom.TickLabelStyle.FontName = fontName;
        plot.Axes.Left.TickLabelStyle.FontName = fontName;
        plot.Axes.Title.Label.FontName = fontName;
        plot.Axes.Bottom.Label.FontName = fontName;
        plot.Axes.Left.Label.FontName = fontName;
        plot.Legend.FontName = fontName;
    }

    private static void ApplyValueLabels(BarPlot bars, string fontName, ChartRenderOptions? options)
    {
        bars.ValueLabelStyle.IsVisible = options?.ShowValueLabels ?? true;
        bars.ValueLabelStyle.FontName = fontName;
        bars.ValueLabelStyle.FontSize = 8;
        bars.ValueLabelStyle.ForeColor = Color.FromHex(ReportVisualTheme.Primary);
    }

    private static byte[] ToPng(Plot plot, int width, int height) =>
        plot.GetImageBytes(width, height, ImageFormat.Png);

    private static Color ParseColor(string? hex, string fallback) =>
        Color.FromHex(string.IsNullOrWhiteSpace(hex) ? fallback : hex);

    private static int ComputeHorizontalBarHeight(int itemCount, int requestedHeight) =>
        Math.Clamp(
            itemCount * ReportVisualTheme.HorizontalBarRowHeightPx + ReportVisualTheme.HorizontalBarBaseHeightPx,
            Math.Min(requestedHeight, 280),
            1400);

    private static string FormatLabel(string label, ChartRenderOptions? options)
    {
        var max = options?.MaxLabelLength;
        if (max is null or <= 0 || label.Length <= max)
            return label;
        return label[..(max.Value - 1)] + "…";
    }

    public byte[] RenderHorizontalBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var labels = items.Select(i => i.Label).ToArray();
        var plot = CreatePlot(title, labels, Opt(options) with { XAxisLabel = options?.XAxisLabel ?? "Значение" });
        if (items.Count == 0)
            return ToPng(plot, width, height);

        var fontName = plot.Axes.Left.TickLabelStyle.FontName;
        var values = items.Select(i => i.Value).ToArray();
        var positions = Enumerable.Range(0, items.Count).Select(i => (double)i).ToArray();
        var bars = plot.Add.Bars(positions, values);
        for (var i = 0; i < items.Count; i++)
            bars.Bars[i].FillColor = ParseColor(items[i].Color, ReportVisualTheme.AccentA);
        bars.Horizontal = true;
        ApplyValueLabels(bars, fontName, options);
        plot.Axes.Left.SetTicks(positions, labels.Select(l => FormatLabel(l, options)).ToArray());
        plot.Axes.Margins(left: 0.25);

        var chartHeight = ComputeHorizontalBarHeight(items.Count, height);
        return ToPng(plot, width, chartHeight);
    }

    public byte[] RenderVerticalBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var labels = items.Select(i => i.Label).ToArray();
        var plot = CreatePlot(title, labels, Opt(options) with { YAxisLabel = options?.YAxisLabel ?? "Значение" });
        if (items.Count == 0)
            return ToPng(plot, width, height);

        var fontName = plot.Axes.Bottom.TickLabelStyle.FontName;
        var values = items.Select(i => i.Value).ToArray();
        var positions = Enumerable.Range(0, items.Count).Select(i => (double)i).ToArray();
        var bars = plot.Add.Bars(positions, values);
        for (var i = 0; i < items.Count; i++)
            bars.Bars[i].FillColor = ParseColor(items[i].Color, ReportVisualTheme.AccentA);
        ApplyValueLabels(bars, fontName, options);
        plot.Axes.Bottom.SetTicks(positions, labels.Select(l => FormatLabel(l, options)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = labels.Any(l => l.Length > 12) ? 45 : 0;
        plot.Axes.Margins(bottom: labels.Any(l => l.Length > 12) ? 0.2 : 0.1);

        return ToPng(plot, width, height);
    }

    public byte[] RenderGroupedBarChart(
        string title,
        IReadOnlyList<ChartGroupedBarItem> items,
        string legendA,
        string legendB,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var labels = items.Select(i => i.Label).ToArray();
        var plot = CreatePlot(title, labels.Concat([legendA, legendB]), Opt(options) with { YAxisLabel = options?.YAxisLabel ?? "Часы" });
        if (items.Count == 0)
            return ToPng(plot, width, height);

        var positions = Enumerable.Range(0, items.Count).Select(i => (double)i).ToArray();
        var valuesA = items.Select(i => i.ValueA).ToArray();
        var valuesB = items.Select(i => i.ValueB).ToArray();
        var barsA = plot.Add.Bars(positions.Select(p => p - 0.15).ToArray(), valuesA);
        var barsB = plot.Add.Bars(positions.Select(p => p + 0.15).ToArray(), valuesB);
        foreach (var bar in barsA.Bars)
            bar.FillColor = Color.FromHex(ReportVisualTheme.AccentA);
        foreach (var bar in barsB.Bars)
            bar.FillColor = Color.FromHex(ReportVisualTheme.AccentB);
        plot.Axes.Bottom.SetTicks(positions, labels.Select(l => FormatLabel(l, options)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = labels.Any(l => l.Length > 12) ? 45 : 0;
        plot.Axes.Margins(bottom: labels.Any(l => l.Length > 12) ? 0.2 : 0.1);
        plot.Legend.IsVisible = true;
        plot.Legend.Alignment = Alignment.UpperRight;
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = legendA, FillColor = Color.FromHex(ReportVisualTheme.AccentA) });
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = legendB, FillColor = Color.FromHex(ReportVisualTheme.AccentB) });

        return ToPng(plot, width, height);
    }

    public byte[] RenderDeltaBarChart(
        string title,
        IReadOnlyList<ChartBarItem> items,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var colored = items
            .Select(i => new ChartBarItem(i.Label, i.Value, ReportVisualTheme.DeltaColor((decimal)i.Value)))
            .ToList();
        var deltaOptions = Opt(options) with { YAxisLabel = options?.YAxisLabel ?? "Δ" };
        if (items.Count == 0)
            return RenderVerticalBarChart(title, colored, width, height, deltaOptions);

        var labels = items.Select(i => i.Label).ToArray();
        var plot = CreatePlot(title, labels, deltaOptions);
        var positions = Enumerable.Range(0, items.Count).Select(i => (double)i).ToArray();
        var values = colored.Select(i => i.Value).ToArray();
        var bars = plot.Add.Bars(positions, values);
        for (var i = 0; i < colored.Count; i++)
            bars.Bars[i].FillColor = ParseColor(colored[i].Color, ReportVisualTheme.AccentA);
        ApplyValueLabels(bars, plot.Axes.Bottom.TickLabelStyle.FontName, options);
        plot.Axes.Bottom.SetTicks(positions, labels.Select(l => FormatLabel(l, options)).ToArray());
        plot.Axes.Bottom.TickLabelStyle.Rotation = labels.Any(l => l.Length > 12) ? 45 : 0;
        var zeroLine = plot.Add.HorizontalLine(0);
        zeroLine.LineWidth = 1;
        zeroLine.Color = Color.FromHex(ReportVisualTheme.Muted);
        plot.Axes.Margins(bottom: labels.Any(l => l.Length > 12) ? 0.2 : 0.1);

        return ToPng(plot, width, height);
    }

    public byte[] RenderMultiLineChart(
        string title,
        IReadOnlyList<ChartLineSeries> series,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var names = series.Select(s => s.Name);
        var plot = CreatePlot(title, names, Opt(options) with
        {
            XAxisLabel = options?.XAxisLabel ?? "Дата",
            YAxisLabel = options?.YAxisLabel ?? "Значение",
        });

        var hasData = false;
        foreach (var s in series)
        {
            if (s.Points.Count == 0) continue;
            hasData = true;
            var xs = s.Points.Select(p => p.X.ToOADate()).ToArray();
            var ys = s.Points.Select(p => p.Y).ToArray();
            var scatter = plot.Add.Scatter(xs, ys);
            scatter.Color = ParseColor(s.Color, ReportVisualTheme.AccentA);
            scatter.LineWidth = 2.5f;
            scatter.MarkerSize = 6;
            scatter.MarkerStyle.OutlineColor = Colors.White;
            scatter.MarkerStyle.OutlineWidth = 1.2f;
            scatter.LegendText = s.Name;
        }

        if (hasData)
        {
            plot.Axes.DateTimeTicksBottom();
            plot.Legend.IsVisible = series.Count > 1;
            plot.Legend.Alignment = Alignment.UpperLeft;
        }

        return ToPng(plot, width, height);
    }

    public byte[] RenderAreaChart(
        string title,
        ChartAreaSeries series,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var plot = CreatePlot(title, [series.Name], Opt(options) with
        {
            XAxisLabel = options?.XAxisLabel ?? "Дата",
            YAxisLabel = options?.YAxisLabel ?? "Накопительно, ч",
        });

        if (series.Points.Count == 0)
            return ToPng(plot, width, height);

        var xs = series.Points.Select(p => p.X.ToOADate()).ToArray();
        var ys = series.Points.Select(p => p.Y).ToArray();
        var coords = new List<Coordinates> { new(xs[0], 0) };
        for (var i = 0; i < xs.Length; i++)
            coords.Add(new Coordinates(xs[i], ys[i]));
        coords.Add(new Coordinates(xs[^1], 0));
        var polygon = plot.Add.Polygon(coords.ToArray());
        polygon.FillColor = ParseColor(series.Color, ReportVisualTheme.AccentA).WithAlpha(0.35);
        polygon.LineColor = ParseColor(series.Color, ReportVisualTheme.AccentA);
        var line = plot.Add.Scatter(xs, ys);
        line.Color = ParseColor(series.Color, ReportVisualTheme.AccentA);
        line.LineWidth = 2.5f;
        line.MarkerSize = 0;
        plot.Axes.DateTimeTicksBottom();
        plot.Legend.IsVisible = true;
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = series.Name, LineColor = ParseColor(series.Color, ReportVisualTheme.AccentA) });

        return ToPng(plot, width, height);
    }

    public byte[] RenderScenarioTotalsChart(
        string title,
        double baseHours,
        double optimisticHours,
        double pessimisticHours,
        int width = ReportVisualTheme.ChartWidth,
        int height = ReportVisualTheme.ChartHeight,
        ChartRenderOptions? options = null)
    {
        var items = new[]
        {
            new ChartBarItem("Базовый", baseHours, ReportVisualTheme.AccentA),
            new ChartBarItem("Оптимист.", optimisticHours, ReportVisualTheme.Positive),
            new ChartBarItem("Пессимист.", pessimisticHours, ReportVisualTheme.Negative),
        };
        return RenderVerticalBarChart(
            title,
            items,
            width,
            height,
            Opt(options) with
            {
                XAxisLabel = options?.XAxisLabel ?? "Сценарий",
                YAxisLabel = options?.YAxisLabel ?? "Трудоёмкость, ч",
            });
    }
}
