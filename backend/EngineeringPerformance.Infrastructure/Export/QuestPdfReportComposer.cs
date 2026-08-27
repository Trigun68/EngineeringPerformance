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
<<<<<<< HEAD
        container
            .Background(ReportVisualTheme.CoverGradientTop)
=======
        // QuestPDF 2024.12 не поддерживает градиентные заливки — сплошной тёмный фон обложки
        container
            .Background(Color.FromHex(ReportVisualTheme.CoverGradientBottom))
>>>>>>> origin/master
            .Padding(40)
            .Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(52).Height(52)
                        .Background(ReportVisualTheme.WatermarkGray)
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
<<<<<<< HEAD
                        e, "ДАТА ФОРМИРОВАНИЯ", $"{generatedAtUtc:dd.MM.yyyy HH:mm} UTC"));
=======
                        e, "ДАТА ФОРМИРОВАНИЯ", $"{generatedAtUtc:dd.MM.yyyy HH:mm} MSK"));
>>>>>>> origin/master
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
<<<<<<< HEAD
                row.RelativeItem().AlignRight().Text($"{generatedAtUtc:yyyy-MM-dd HH:mm} UTC")
=======
                row.RelativeItem().AlignRight().Text($"{generatedAtUtc:yyyy-MM-dd HH:mm} MSK")
>>>>>>> origin/master
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
    // Сводка «план/факт» (вкладка «Отчёт»)
    // ------------------------------------------------------------------

<<<<<<< HEAD
    /// <summary>Цвет акцента блока «План» — паритет client/src/pages/projects/reports/reportCalc.ts::ACCENT_PLAN.</summary>
    public const string PlanAccent = "#3498DB";

    /// <summary>Цвет акцента блока «Факт» — паритет ACCENT_FACT.</summary>
    public const string FactAccent = "#2C3E50";

    /// <summary>Двухколоночная сводка «план/факт» — аналог PlanFactSummary.tsx на портале.</summary>
=======
    // Акценты плана/факта — паритет client reportCalc.ts (ACCENT_PLAN / ACCENT_FACT).
    public const string PlanAccent = "#3498DB";
    public const string FactAccent = "#2C3E50";

>>>>>>> origin/master
    public static void ComposePlanFactSummary(
        IContainer container,
        PdfPlanFactColumn plan,
        PdfPlanFactColumn? fact,
        string? deltaCaption)
    {
        container.Row(row =>
        {
            row.Spacing(10);
            row.RelativeItem().Element(e => ComposePlanFactColumn(e, "ПЛАН", PlanAccent, plan, null));
            row.RelativeItem().Element(e => ComposePlanFactColumn(
                e, "ФАКТ", fact is null ? ReportVisualTheme.Muted : FactAccent,
                fact ?? new PdfPlanFactColumn("Основная версия факта не назначена", "—", "—", "—", "Н/Д"),
                fact is null ? null : deltaCaption));
        });
    }

    private static void ComposePlanFactColumn(
        IContainer container, string kicker, string accentColor, PdfPlanFactColumn column, string? deltaCaption)
    {
        container.Background(Colors.White).BorderLeft(3).BorderColor(accentColor).Padding(12).Column(col =>
        {
            col.Spacing(6);
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(kicker).SemiBold().FontSize(9).FontColor(accentColor);
                r.RelativeItem().AlignRight().Text(column.VersionLabel).FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
            });
            col.Item().Row(r =>
            {
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("Трудоёмкость").FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
                    c.Item().Text($"{column.Hours} ч").SemiBold().FontSize(17).FontColor(ReportVisualTheme.Primary);
                });
                r.RelativeItem().Column(c =>
                {
                    c.Item().Text("Z-оценка").FontSize(7.5f).FontColor(ReportVisualTheme.Muted);
                    c.Item().Text(column.ZScore).SemiBold().FontSize(17)
                        .FontColor(ReportVisualTheme.InterpretationColor(column.Interpretation));
                });
            });
            col.Item().Text($"{column.Interpretation}{(string.IsNullOrWhiteSpace(column.Percentile) ? "" : $" · перцентиль {column.Percentile}")}")
                .FontSize(8.5f).FontColor(ReportVisualTheme.InterpretationColor(column.Interpretation));
            if (!string.IsNullOrWhiteSpace(deltaCaption))
                col.Item().Text(deltaCaption).FontSize(8).FontColor(ReportVisualTheme.Muted);
        });
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

<<<<<<< HEAD
    /// <summary>��кала Excellent…Critical (±1σ). Если передан <paramref name="currentZ"/> — под шкалой рисуется маркер положения.</summary>
=======
    /// <summary>Шкала Excellent…Critical (±1σ). Если передан <paramref name="currentZ"/> — под шкалой рисуется маркер положения.</summary>
>>>>>>> origin/master
    public static void ComposeZScoreScaleLegend(IContainer container, decimal? currentZ = null)
    {
        container.Column(col =>
        {
            col.Spacing(3);
<<<<<<< HEAD
            col.Item().Text("Шкал�� оценки (Z-score)").SemiBold().FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
=======
            col.Item().Text("Шкала оценки (Z-score)").SemiBold().FontSize(ReportVisualTheme.FontH2).FontColor(ReportVisualTheme.Primary);
>>>>>>> origin/master
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
<<<<<<< HEAD
                col.Item().PaddingTop(2).Element(e => ComposeZScoreMarker(e, currentZ.Value, "Z", ReportVisualTheme.Primary, ReportVisualTheme.GlyphUp));
        });
    }

    /// <summary>Шкала с двумя маркерами — план (тонкий) и факт (крупный, акцентный). Аналог ZScale.tsx с двумя значениями.</summary>
    public static void ComposeZScoreScaleLegend(IContainer container, decimal? planZ, decimal? factZ)
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
            if (planZ.HasValue)
                col.Item().PaddingTop(2).Element(e => ComposeZScoreMarker(e, planZ.Value, "План", ReportVisualTheme.Muted, ReportVisualTheme.GlyphDot, small: true));
            if (factZ.HasValue)
                col.Item().PaddingTop(1).Element(e => ComposeZScoreMarker(e, factZ.Value, "Факт", ReportVisualTheme.Primary, ReportVisualTheme.GlyphUp));
            if (!planZ.HasValue && !factZ.HasValue)
                col.Item().PaddingTop(2).Text("Нет данных для отображения маркера.").FontSize(7.5f).Italic().FontColor(ReportVisualTheme.Muted);
        });
    }

    private static void ComposeZScoreMarker(
        IContainer container, decimal z, string label, string color, string glyph, bool small = false)
=======
                col.Item().PaddingTop(2).Element(e => ComposeZScoreMarker(e, currentZ.Value));
        });
    }

    private static void ComposeZScoreMarker(IContainer container, decimal z)
>>>>>>> origin/master
    {
        // Диапазон шкалы визуально соответствует [-2, 2]; клэмп + перевод в долю 0..1.
        var clamped = Math.Max(-2m, Math.Min(2m, z));
        var proportion = (double)((clamped + 2m) / 4m);
        var left = Math.Clamp(proportion, 0.02, 0.94);
        var right = 1 - left;
<<<<<<< HEAD
        var markerWidth = small ? 46 : 40;
        var glyphSize = small ? 8f : 10f;
=======
>>>>>>> origin/master

        container.Row(row =>
        {
            row.RelativeItem((float)left);
<<<<<<< HEAD
            row.ConstantItem(markerWidth).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(glyph).FontSize(glyphSize).FontColor(color);
                c.Item().AlignCenter().Text($"{label} {z:N2}").FontSize(small ? 6.5f : 7).FontColor(ReportVisualTheme.Muted);
=======
            row.ConstantItem(36).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(ReportVisualTheme.GlyphUp).FontSize(10).FontColor(ReportVisualTheme.Primary);
                c.Item().AlignCenter().Text($"Z = {z:N2}").FontSize(7).FontColor(ReportVisualTheme.Muted);
>>>>>>> origin/master
            });
            row.RelativeItem((float)right);
        });
    }

<<<<<<< HEAD
    // ------------------------------------------------------------------
    // Графики
    // ------------------------------------------------------------------

    public static void ComposeChartImage(IContainer container, byte[]? pngBytes, string? caption = null)
=======
    /// <summary>Шкала с двумя маркерами (план/факт) — для сводки «Отчёт».</summary>
    public static void ComposeZScoreScaleLegend(IContainer container, double? planZ, double? factZ)
>>>>>>> origin/master
    {
        container.Column(col =>
        {
            col.Spacing(4);
            col.Item().Row(row =>
            {
                row.Spacing(2);
                foreach (var (label, _, color, _) in ZScoreBands)
                    row.RelativeItem().Background(color).Height(16).AlignMiddle().AlignCenter()
                        .Text(label).FontSize(7).FontColor(Colors.White);
            });
            if (planZ.HasValue)
                col.Item().Element(e => ComposeZScoreMarker(e, planZ.Value, "ПЛАН", PlanAccent, markerHeight: 14));
            if (factZ.HasValue)
                col.Item().Element(e => ComposeZScoreMarker(e, factZ.Value, "ФАКТ", FactAccent, markerHeight: 18));
            col.Item().Row(row =>
            {
                row.Spacing(2);
                foreach (var (_, range, _, _) in ZScoreBands)
                    row.RelativeItem().AlignCenter().Text(range).FontSize(7).FontColor(ReportVisualTheme.Muted);
            });
        });
    }

    private static void ComposeZScoreMarker(
        IContainer container, double z, string label, string color, float markerHeight)
    {
        var position = (float)((z + 3.0) / 6.0);
        position = Math.Clamp(position, 0f, 1f);
        container.Height(30).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem(position).Text("");
                r.ConstantItem(60).AlignCenter().Text(label).SemiBold().FontSize(8).FontColor(color);
                r.RelativeItem(1f - position).Text("");
            });
            col.Item().Row(r =>
            {
                r.RelativeItem(position).Text("");
                r.ConstantItem(3).Background(color).Height(markerHeight);
                r.RelativeItem(1f - position).Text("");
            });
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
<<<<<<< HEAD
                    var rowBg = rowIndex % 2 == 1 ? ReportVisualTheme.SectionBg : (string)Colors.White;
=======
                    var rowBg = rowIndex % 2 == 1 ? Color.FromHex(ReportVisualTheme.SectionBg) : Colors.White;
>>>>>>> origin/master
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
        container.AlignLeft().Background(color)
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
<<<<<<< HEAD
        var generatedAt = DateTime.UtcNow;
=======
        // Отображаемое время формирования — московское, как в шапках табличных отчётов
        var generatedAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime;
>>>>>>> origin/master

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
