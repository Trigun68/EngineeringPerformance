using EngineeringPerformance.Application.Export;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Общие блоки QuestPDF для визуальных отчётов — дизайн-система «Executive Technical».</summary>
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

    // ------------------------------------------------------------------
    // Титульная страница
    // ------------------------------------------------------------------

    public static void ComposeCoverPage(IContainer container, PdfCoverInfo cover, DateTime generatedAtUtc)
    {
        container
            .BackgroundLinearGradient(135, new[]
            {
                Color.FromHex(ReportVisualTheme.CoverGradientTop),
                Color.FromHex(ReportVisualTheme.CoverGradientBottom),
            })
            .Padding(40)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(52).Height(52)
                        .Background(ReportVisualTheme.WatermarkGray).CornerRadius(26)
                        .AlignCenter().AlignMiddle()
                        .Text("EP").Bold().FontSize(18).FontColor(Colors.White);
                    row.RelativeItem();
                    row.ConstantItem(150).AlignRight().AlignTop().Element(e => ComposeKindBadge(e, cover.Kind));
                });

                col.Item().PaddingTop(36).Text(cover.ReportTitle)
                    .FontSize(ReportVisualTheme.FontCoverTitle).Bold().FontColor(Colors.White);

                col.Item().PaddingTop(4)
                    .Text("Engineering Performance · Инженерная аналитика трудоёмкости")
                    .FontSize(ReportVisualTheme.FontCoverSubtitle).FontColor(ReportVisualTheme.CoverMutedText);

                col.Item().PaddingTop(28).Element(e => ComposeCoverMetaCard(e, cover, generatedAtUtc));

                if (cover.TeaserKpis.Count > 0)
                    col.Item().PaddingTop(24).Element(e => ComposeCoverTeaserKpis(e, cover.TeaserKpis));

                col.Item().PaddingTop(20)
                    .Text("Engineering Performance · Confidential")
                    .FontSize(8).FontColor(ReportVisualTheme.CoverFooterText);
            });
    }

    private static void ComposeKindBadge(IContainer container, PdfReportKind kind)
    {
        container
            .Background(ReportVisualTheme.ReportKindColor(kind))
            .CornerRadius(3)
            .PaddingHorizontal(9).PaddingVertical(5)
            .AlignCenter()
            .Text(ReportVisualTheme.ReportKindLabel(kind)).FontSize(7.5f).SemiBold().FontColor(Colors.White);
    }

    private static void ComposeCoverMetaCard(IContainer container, PdfCoverInfo cover, DateTime generatedAtUtc)
    {
        container
            .Background(ReportVisualTheme.CoverMetaCardBg)
            .BorderLeft(2).BorderColor(ReportVisualTheme.AccentA)
            .Padding(16)
            .Column(col =>
            {
                col.Spacing(8);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => ComposeCoverMetaField(e, "ПРОЕКТ", cover.Project));
                    row.RelativeItem().Element(e => ComposeCoverMetaField(e, "ВЕРСИЯ", cover.VersionLabel));
                });
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => ComposeCoverMetaField(
                        e, "ДАТА ФОРМИРОВАНИЯ", $"{generatedAtUtc:dd.MM.yyyy HH:mm} UTC"));
                    row.RelativeItem().Element(e => ComposeCoverMetaField(e, "СИСТЕМА", "Engineering Performance"));
                });
                foreach (var line in cover.ExtraMeta ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    col.Item().Text(line).FontSize(8.5f).FontColor(ReportVisualTheme.CoverBodyText);
                }
            });
    }

    private static void ComposeCoverMetaField(IContainer container, string label, string value)
    {
        container.Column(c =>
        {
            c.Item().Text(label).FontSize(7).FontColor(ReportVisualTheme.CoverMutedText);
            c.Item().PaddingTop(2).Text(value).SemiBold().FontSize(10.5f).FontColor(Colors.White);
        });
    }

    private static void ComposeCoverTeaserKpis(IContainer container, IReadOnlyList<ReportKpiItem> kpis)
    {
        container.Row(row =>
        {
            row.Spacing(18);
            foreach (var item in kpis)
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(item.Label.ToUpperInvariant()).FontSize(7.5f).FontColor(ReportVisualTheme.CoverMutedText);
                    c.Item().PaddingTop(2).Text(item.Value).Bold().FontSize(20).FontColor(Colors.White);
                    c.Item().PaddingTop(5).Height(2).Width(28).Background(ReportVisualTheme.AccentA);
                });
            }
        });
    }

    // ------------------------------------------------------------------
    // Хедер / футер контентных страниц
    // ------------------------------------------------------------------

    public static void ComposeHeader(
        IContainer container,
        PdfReportKind kind,
        string title,
        string project,
        string versionLabel,
        IReadOnlyList<string>? extraMeta = null)
    {
        container.PaddingBottom(6).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(title).SemiBold().FontSize(ReportVisualTheme.FontH1).FontColor(ReportVisualTheme.Primary);
                    c.Item().PaddingTop(1).Text($"{project} · {versionLabel}")
                        .FontSize(8.5f).FontColor(ReportVisualTheme.Muted);
                });
                row.ConstantItem(150).AlignRight().AlignMiddle().Element(e => ComposeKindBadge(e, kind));
            });
            if (extraMeta is { Count: > 0 })
                col.Item().PaddingTop(2).Text(string.Join("   ·   ", extraMeta.Where(m => !string.IsNullOrWhiteSpace(m))))
                    .FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
            col.Item().PaddingTop(6).Height(2).Background(ReportVisualTheme.AccentA);
        });
    }

    public static void ComposeFooter(IContainer container, DateTime generatedAtUtc)
    {
        container.Column(col =>
        {
            col.Item().Height(1).Background(ReportVisualTheme.RuleColor);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text("Engineering Performance · Confidential")
                    .FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
                row.RelativeItem().AlignCenter().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(ReportVisualTheme.Muted));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
                row.RelativeItem().AlignRight().Text($"{generatedAtUtc:yyyy-MM-dd HH:mm} UTC")
                    .FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
            });
        });
    }

    // ------------------------------------------------------------------
    // KPI-карточки
    // ------------------------------------------------------------------

    public static void ComposeKpiStrip(IContainer container, IReadOnlyList<ReportKpiItem> items)
    {
        if (items.Count == 0) return;

        container.Row(row =>
        {
            row.Spacing(8);
            foreach (var item in items)
            {
                var isStatus = item.Label.Contains("Оценка", StringComparison.OrdinalIgnoreCase)
                    || item.Label.Contains("Интерпрет", StringComparison.OrdinalIgnoreCase);
                row.RelativeItem().Background(Colors.White).Padding(10).Column(c =>
                {
                    c.Item().Text(item.Label.ToUpperInvariant()).FontSize(ReportVisualTheme.FontKpiLabel).FontColor(ReportVisualTheme.Muted);
                    c.Item().PaddingTop(3).Row(r =>
                    {
                        if (isStatus)
                            r.AutoItem().PaddingRight(5).AlignMiddle()
                                .Text(ReportVisualTheme.GlyphDot).FontSize(11)
                                .FontColor(ReportVisualTheme.InterpretationColor(item.Value));
                        r.RelativeItem().Text(item.Value).SemiBold().FontSize(ReportVisualTheme.FontKpiValue).FontColor(ReportVisualTheme.Primary);
                    });
                    c.Item().PaddingTop(6).Height(2).Background(ReportVisualTheme.AccentA);
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

    // ------------------------------------------------------------------
    // Шкала Z-score
    // ------------------------------------------------------------------

    private static readonly (string Label, string Range, string Color, decimal LowerBound)[] ZScoreBands =
    [
        ("Отлично", "Z < −1", ReportVisualTheme.Positive, -2m),
        ("Хорошо", "−1…−0.5", ReportVisualTheme.Positive, -1m),
        ("Удовл.", "−0.5…0.5", ReportVisualTheme.Neutral, -0.5m),
        ("Внимание", "0.5…1", ReportVisualTheme.Negative, 0.5m),
        ("Критично", "Z > 1", ReportVisualTheme.Negative, 1m),
    ];

    /// <summary>Шкала Excellent…Critical (±1σ). Если передан <paramref name="currentZ"/> — под шкалой рисуется маркер положения.</summary>
    public static void ComposeZScoreScaleLegend(IContainer container, decimal? currentZ = null)
    {
        container.Column(col =>
        {
            col.Spacing(3);
            col.Item().Text("Шкала оценки (Z-score)").SemiBold().FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
            col.Item().Row(row =>
            {
                row.Spacing(2);
                foreach (var (label, range, color, _) in ZScoreBands)
                {
                    row.RelativeItem().Background(color).Padding(6).Column(c =>
                    {
                        c.Item().AlignCenter().Text(label).SemiBold().FontSize(8).FontColor(Colors.White);
                        c.Item().AlignCenter().Text(range).FontSize(7).FontColor(Colors.White);
                    });
                }
            });
            if (currentZ.HasValue)
                col.Item().PaddingTop(2).Element(e => ComposeZScoreMarker(e, currentZ.Value));
        });
    }

    private static void ComposeZScoreMarker(IContainer container, decimal z)
    {
        // Диапазон шкалы визуально соответствует [-2, 2]; клэмп + перевод в долю 0..1.
        var clamped = Math.Max(-2m, Math.Min(2m, z));
        var proportion = (double)((clamped + 2m) / 4m);
        var left = Math.Clamp(proportion, 0.02, 0.94);
        var right = 1 - left;

        container.Row(row =>
        {
            row.RelativeItem((float)left);
            row.ConstantItem(36).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(ReportVisualTheme.GlyphUp).FontSize(10).FontColor(ReportVisualTheme.Primary);
                c.Item().AlignCenter().Text($"Z = {z:N2}").FontSize(7).FontColor(ReportVisualTheme.Muted);
            });
            row.RelativeItem((float)right);
        });
    }

    // ------------------------------------------------------------------
    // Графики
    // ------------------------------------------------------------------

    public static void ComposeChartImage(IContainer container, string title, byte[]? pngBytes, string? caption = null)
    {
        container.Column(col =>
        {
            col.Spacing(4);
            if (!string.IsNullOrWhiteSpace(title))
                col.Item().Text(title).SemiBold().FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
            if (pngBytes is { Length: > 0 })
                col.Item().Image(pngBytes).FitWidth();
            else
                col.Item().Text("Нет данных для графика").Italic().FontColor(ReportVisualTheme.Muted);
            if (!string.IsNullOrWhiteSpace(caption))
                col.Item().Text(caption).FontSize(ReportVisualTheme.FontCaption).FontColor(ReportVisualTheme.Muted);
        });
    }

    // ------------------------------------------------------------------
    // Callout-блоки (авто-выводы)
    // ------------------------------------------------------------------

    private static (string Border, string Bg, string Glyph) CalloutStyle(PdfCalloutKind kind) => kind switch
    {
        PdfCalloutKind.Risk => (ReportVisualTheme.Negative, ReportVisualTheme.RiskBg, ReportVisualTheme.GlyphWarning),
        PdfCalloutKind.Attention => (ReportVisualTheme.Neutral, ReportVisualTheme.AttentionBg, ReportVisualTheme.GlyphWarning),
        PdfCalloutKind.Strength => (ReportVisualTheme.Positive, ReportVisualTheme.StrengthBg, ReportVisualTheme.GlyphCheck),
        _ => (ReportVisualTheme.Muted, ReportVisualTheme.DataBg, ReportVisualTheme.GlyphDot),
    };

    public static void ComposeCalloutBlock(IContainer container, PdfCallout callout)
    {
        var (borderColor, bg, glyph) = CalloutStyle(callout.Kind);
        container.Background(bg).BorderTop(2).BorderColor(borderColor).Padding(9).Row(row =>
        {
            row.AutoItem().PaddingRight(7).Text(glyph).FontSize(12).FontColor(borderColor).Bold();
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(callout.Title).SemiBold().FontSize(9.5f).FontColor(ReportVisualTheme.Primary);
                col.Item().PaddingTop(2).Text(callout.Detail).FontSize(8.5f).FontColor(ReportVisualTheme.Muted);
            });
        });
    }

    public static void ComposeCalloutList(IContainer container, IReadOnlyList<PdfCallout> callouts)
    {
        if (callouts.Count == 0) return;
        container.Column(col =>
        {
            col.Spacing(6);
            foreach (var callout in callouts)
                col.Item().Element(c => ComposeCalloutBlock(c, callout));
        });
    }

    // ------------------------------------------------------------------
    // Таблицы
    // ------------------------------------------------------------------

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
                        header.Cell().Background(Colors.White).BorderBottom(2).BorderColor(ReportVisualTheme.AccentA)
                            .PaddingVertical(5).PaddingHorizontal(4)
                            .Text(h).SemiBold().FontSize(8).FontColor(ReportVisualTheme.Primary);
                });
                for (var rowIndex = 0; rowIndex < data.Count; rowIndex++)
                {
                    var row = data[rowIndex];
                    var rowBg = rowIndex % 2 == 1 ? ReportVisualTheme.SectionBg : Colors.White;
                    for (var i = 0; i < headers.Length; i++)
                    {
                        var raw = i < row.Cells.Length ? row.Cells[i] ?? "—" : "—";
                        var cell = table.Cell().Background(rowBg).BorderBottom(0.5f).BorderColor(ReportVisualTheme.RuleColor)
                            .PaddingVertical(4).PaddingHorizontal(4);
                        var isInterp = interpretationColumnIndex == i
                            || (row.InterpretationText is not null && i == headers.Length - 1 && interpretationColumnIndex is null);

                        if (i == 0 && row.Level > 0)
                        {
                            cell.Element(e => ComposeIndentedLabel(e, raw, row.Level));
                        }
                        else if (isInterp)
                        {
                            var color = ReportVisualTheme.InterpretationColor(row.InterpretationText ?? raw);
                            cell.Element(e => ComposeInterpretationChip(e, raw, color));
                        }
                        else
                        {
                            cell.Text(raw).FontSize(8).FontColor(ReportVisualTheme.Primary);
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

    private static void ComposeIndentedLabel(IContainer container, string text, int level)
    {
        container.Row(row =>
        {
            row.ConstantItem(level * 10);
            row.ConstantItem(2).Height(11).Background(ReportVisualTheme.RuleColor);
            row.ConstantItem(4);
            row.RelativeItem().Text(text).FontSize(8).FontColor(ReportVisualTheme.Primary);
        });
    }

    private static void ComposeInterpretationChip(IContainer container, string text, string color)
    {
        container.AlignLeft().Background(color).CornerRadius(3)
            .PaddingHorizontal(6).PaddingVertical(2)
            .Text(text).FontSize(7.5f).SemiBold().FontColor(Colors.White);
    }

    /// <summary>Отступ иерархии простыми пробелами (для случаев без визуального рендера ячейки).</summary>
    public static string IndentLabel(string name, int level) =>
        level <= 0 ? name : new string(' ', level * ReportVisualTheme.HierarchyIndentSpaces) + name;

    // ------------------------------------------------------------------
    // Сборка документа
    // ------------------------------------------------------------------

    private static TextStyle DefaultBodyStyle()
    {
        var style = TextStyle.Default.FontSize(ReportVisualTheme.FontBody).FontColor(ReportVisualTheme.Primary);
        return ApplyFont(style);
    }

    private static byte[] BuildReport(
        PageSize pageSize,
        float margin,
        Action<ColumnDescriptor> buildContent,
        PdfReportKind kind,
        string reportTitle,
        string project,
        string versionLabel,
        IReadOnlyList<ReportKpiItem>? teaserKpis,
        string[] extraMeta)
    {
        var cover = new PdfCoverInfo(kind, reportTitle, project, versionLabel, teaserKpis ?? [], extraMeta);
        var generatedAt = DateTime.UtcNow;

        var document = Document.Create(documentContainer =>
        {
            documentContainer.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(0);
                page.DefaultTextStyle(DefaultBodyStyle());
                page.Content().Element(c => ComposeCoverPage(c, cover, generatedAt));
            });

            documentContainer.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(margin);
                page.DefaultTextStyle(DefaultBodyStyle());
                page.Header().Element(c => ComposeHeader(c, kind, reportTitle, project, versionLabel, extraMeta));
                page.Content().PaddingVertical(8).Background(ReportVisualTheme.PageBg).Padding(8).Column(buildContent);
                page.Footer().Element(c => ComposeFooter(c, generatedAt));
            });
        });
        return document.GeneratePdf();
    }

    public static byte[] BuildLandscapeReport(
        Action<ColumnDescriptor> buildContent,
        PdfReportKind kind,
        string reportTitle,
        string project,
        string versionLabel,
        IReadOnlyList<ReportKpiItem>? teaserKpis = null,
        params string[] extraMeta) =>
        BuildReport(PageSizes.A4.Landscape(), 24, buildContent, kind, reportTitle, project, versionLabel, teaserKpis, extraMeta);

    public static byte[] BuildPortraitReport(
        Action<ColumnDescriptor> buildContent,
        PdfReportKind kind,
        string reportTitle,
        string project,
        string versionLabel,
        IReadOnlyList<ReportKpiItem>? teaserKpis = null,
        params string[] extraMeta) =>
        BuildReport(PageSizes.A4, 28, buildContent, kind, reportTitle, project, versionLabel, teaserKpis, extraMeta);
}
