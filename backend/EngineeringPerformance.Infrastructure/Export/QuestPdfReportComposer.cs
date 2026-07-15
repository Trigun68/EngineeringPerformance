using EngineeringPerformance.Application.Export;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Общие блоки QuestPDF для визуальных отчётов (стиль портала).</summary>
public static class QuestPdfReportComposer
{
    private static readonly string? EmbeddedFontPath = ResolveEmbeddedFontPath();
    private static bool _fontRegistered;

    static QuestPdfReportComposer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        TryRegisterFont();
    }

    private static string? ResolveEmbeddedFontPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, ReportVisualTheme.EmbeddedFontRelativePath),
            Path.Combine(baseDir, "Assets", "Fonts", "DejaVuSans.ttf"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ReportVisualTheme.EmbeddedFontRelativePath)),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static void TryRegisterFont()
    {
        if (_fontRegistered || EmbeddedFontPath is null) return;
        try
        {
            using var stream = File.OpenRead(EmbeddedFontPath);
            FontManager.RegisterFont(stream);
            _fontRegistered = true;
        }
        catch
        {
            // Fallback на системный шрифт — лучше, чем падение экспорта.
        }
    }

    private static TextStyle ApplyFont(TextStyle style) =>
        _fontRegistered ? style.FontFamily(ReportVisualTheme.EmbeddedFontName) : style;

    public static string? FontPathForTests => EmbeddedFontPath;

    public static void ComposeHeader(IContainer container, string title, params string[] metaLines)
    {
        container.Background(ReportVisualTheme.Primary).Padding(10).Column(col =>
        {
            col.Spacing(2);
            col.Item().Text(title).SemiBold().FontSize(14).FontColor(ReportVisualTheme.HeaderText);
            foreach (var line in metaLines.Where(l => !string.IsNullOrWhiteSpace(l)))
                col.Item().Text(line).FontSize(9).FontColor("#BDC3C7");
        });
    }

    public static void ComposeKpiStrip(IContainer container, IReadOnlyList<ReportKpiItem> items)
    {
        if (items.Count == 0) return;

        container.Row(row =>
        {
            row.Spacing(8);
            foreach (var item in items)
            {
                row.RelativeItem().Background(ReportVisualTheme.CardBg)
                    .Border(1).BorderColor(ReportVisualTheme.AccentA)
                    .Padding(10).Column(c =>
                    {
                        c.Item().Text(item.Label).FontSize(8).FontColor(ReportVisualTheme.Muted);
                        c.Item().Text(item.Value).SemiBold().FontSize(12).FontColor(ReportVisualTheme.Primary);
                    });
            }
        });
    }

    /// <summary>KPI «Оценка проекта» — карточки как на портале.</summary>
    public static void ComposeProjectAssessmentKpis(
        IContainer container,
        string hoursLabel,
        string hoursValue,
        string zValue,
        string percentile,
        string interpretation)
    {
        ComposeKpiStrip(container,
        [
            new ReportKpiItem(hoursLabel, hoursValue),
            new ReportKpiItem("Взвеш. Z", zValue),
            new ReportKpiItem("Перцентиль", string.IsNullOrWhiteSpace(percentile) ? "—" : percentile),
            new ReportKpiItem("Оценка", string.IsNullOrWhiteSpace(interpretation) ? "—" : interpretation),
        ]);
    }

    /// <summary>Шкала Z Excellent…Critical (±1σ).</summary>
    public static void ComposeZScoreScaleLegend(IContainer container)
    {
        var bands = new (string Label, string Range, string Color)[]
        {
            ("Отлично", "Z < −1", ReportVisualTheme.Positive),
            ("Хорошо", "−1…−0.5", ReportVisualTheme.Positive),
            ("Удовл.", "−0.5…0.5", ReportVisualTheme.Neutral),
            ("Внимание", "0.5…1", ReportVisualTheme.Negative),
            ("Критично", "Z > 1", ReportVisualTheme.Negative),
        };

        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Text("Шкала оценки (Z-score)").SemiBold().FontSize(10).FontColor(ReportVisualTheme.Primary);
            col.Item().Row(row =>
            {
                row.Spacing(4);
                foreach (var (label, range, color) in bands)
                {
                    row.RelativeItem().Background(color).Padding(6).Column(c =>
                    {
                        c.Item().AlignCenter().Text(label).SemiBold().FontSize(8).FontColor(Colors.White);
                        c.Item().AlignCenter().Text(range).FontSize(7).FontColor(Colors.White);
                    });
                }
            });
        });
    }

    public static void ComposeChartImage(IContainer container, byte[]? pngBytes, string caption)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            if (pngBytes is { Length: > 0 })
                col.Item().Image(pngBytes).FitWidth();
            else
                col.Item().Text("Нет данных для графика").Italic().FontColor(ReportVisualTheme.Muted);
            if (!string.IsNullOrWhiteSpace(caption))
                col.Item().Text(caption).FontSize(8).FontColor(ReportVisualTheme.Muted);
        });
    }

    public static void ComposeTable(
        IContainer container,
        string sectionTitle,
        string[] headers,
        IEnumerable<string[]> rows,
        int maxRows = ReportVisualTheme.TableMaxRowsUnlimited,
        int? interpretationColumnIndex = null)
    {
        var allRows = rows.Select(r => new HierarchicalPdfRow(0, r)).ToList();
        ComposeHierarchicalTable(container, sectionTitle, headers, allRows, maxRows, interpretationColumnIndex);
    }

    public static void ComposeHierarchicalTable(
        IContainer container,
        string sectionTitle,
        string[] headers,
        IEnumerable<HierarchicalPdfRow> rows,
        int maxRows = ReportVisualTheme.TableMaxRowsUnlimited,
        int? interpretationColumnIndex = null)
    {
        var allRows = rows.ToList();
        var data = maxRows > 0 ? allRows.Take(maxRows).ToList() : allRows;
        container.Column(col =>
        {
            col.Spacing(6);
            if (!string.IsNullOrWhiteSpace(sectionTitle))
                col.Item().Text(sectionTitle).SemiBold().FontSize(11).FontColor(ReportVisualTheme.Primary);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var _ in headers)
                        cols.RelativeColumn();
                });
                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(ReportVisualTheme.Primary).Padding(4)
                            .Text(h).SemiBold().FontSize(8).FontColor(ReportVisualTheme.HeaderText);
                });
                foreach (var row in data)
                {
                    for (var i = 0; i < headers.Length; i++)
                    {
                        var raw = i < row.Cells.Length ? row.Cells[i] ?? "—" : "—";
                        var text = i == 0
                            ? IndentLabel(raw, row.Level)
                            : raw;
                        var cell = table.Cell().BorderBottom(0.5f).BorderColor("#E8E8E8").Padding(3);
                        var isInterp = interpretationColumnIndex == i
                            || (row.InterpretationText is not null && i == headers.Length - 1 && interpretationColumnIndex is null);
                        if (isInterp)
                        {
                            var color = ReportVisualTheme.InterpretationColor(row.InterpretationText ?? text);
                            cell.Text(text).FontSize(8).FontColor(color).SemiBold();
                        }
                        else
                        {
                            cell.Text(text).FontSize(8).FontColor(ReportVisualTheme.Primary);
                        }
                    }
                }
            });
            if (maxRows > 0 && allRows.Count > maxRows)
                col.Item().Text($"Показаны первые {maxRows} из {allRows.Count} строк.").FontSize(7).Italic();
            else if (allRows.Count > 0)
                col.Item().Text($"Всего строк: {allRows.Count}.").FontSize(7).FontColor(ReportVisualTheme.Muted);
        });
    }

    public static string IndentLabel(string name, int level)
    {
        if (level <= 0) return name;
        return new string(' ', level * ReportVisualTheme.HierarchyIndentSpaces) + "└ " + name;
    }

    public static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(t =>
        {
            t.Span("Engineering Performance · ");
            t.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
            t.Span(" UTC · ");
            t.CurrentPageNumber();
            t.Span(" / ");
            t.TotalPages();
        });
    }

    private static TextStyle DefaultBodyStyle()
    {
        var style = TextStyle.Default.FontSize(9).FontColor(ReportVisualTheme.Primary);
        return ApplyFont(style);
    }

    public static byte[] BuildLandscapeReport(Action<ColumnDescriptor> buildContent, string headerTitle, params string[] meta)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(DefaultBodyStyle());
                page.Header().Element(c => ComposeHeader(c, headerTitle, meta));
                page.Content().PaddingVertical(8).Background(ReportVisualTheme.PageBg).Padding(8).Column(buildContent);
                page.Footer().Element(ComposeFooter);
            });
        });
        return document.GeneratePdf();
    }

    public static byte[] BuildPortraitReport(Action<ColumnDescriptor> buildContent, string headerTitle, params string[] meta)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(DefaultBodyStyle());
                page.Header().Element(c => ComposeHeader(c, headerTitle, meta));
                page.Content().PaddingVertical(8).Background(ReportVisualTheme.PageBg).Padding(8).Column(buildContent);
                page.Footer().Element(ComposeFooter);
            });
        });
        return document.GeneratePdf();
    }
}
