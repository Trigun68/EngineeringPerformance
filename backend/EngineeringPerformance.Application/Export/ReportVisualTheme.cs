namespace EngineeringPerformance.Application.Export;

/// <summary>Тип PDF-отчёта — определяет бейдж/подзаголовок на обложке и в хедере.</summary>
public enum PdfReportKind
{
    Forecast,
    Fact,
    Compare,
    Trend,
    Combined,
    Report,
}

/// <summary>Дизайн-токены PDF — «Executive Technical»: глубокий графит + инженерный синий акцент.</summary>
public static class ReportVisualTheme
{
    /// <summary>Основной графитово-синий — обложка/хедер/заголовки.</summary>
    public const string Primary = "#1B2A38";

    /// <summary>Самый тёмный тон — низ градиента обложки.</summary>
    public const string PrimaryDark = "#10181F";

    /// <summary>Инженерный синий акцент — линии, чипы, ключевые данные.</summary>
    public const string AccentA = "#2E86C1";

    public const string AccentDark = "#1B5A85";

    /// <summary>Мягкая заливка на базе AccentA — фоны чипов/карточек KPI.</summary>
    public const string AccentSoft = "#D6EAF8";

    public const string AccentB = "#8E44AD";
    public const string Positive = "#1E8449";
    public const string Negative = "#C0392B";
    public const string Neutral = "#B9770E";

    /// <summary>Приглушённый серо-синий для второстепенного текста.</summary>
    public const string Muted = "#7B8794";

    /// <summary>Светлая карточка KPI.</summary>
    public const string CardBg = "#EBF5FB";

    public const string PageBg = "#F7F9FB";
    public const string HeaderText = "#ECF0F1";

    /// <summary>Тонкие разделительные линии таблиц/футера.</summary>
    public const string RuleColor = "#E3E7EB";

    /// <summary>Фон чётных строк таблиц и секций «через одну».</summary>
    public const string SectionBg = "#F1F4F7";

    /// <summary>Верх/низ вертикального градиента титульной страницы.</summary>
    public const string CoverGradientTop = "#22384A";
    public const string CoverGradientBottom = PrimaryDark;

    /// <summary>Фон водяного знака/монограммы на обложке.</summary>
    public const string WatermarkGray = "#2A3F52";

    // Тёмная палитра обложки (тинты Primary/PrimaryDark — не самостоятельные цвета).
    public const string CoverMetaCardBg = "#1A2B3A";
    public const string CoverMutedText = "#8A97A5";
    public const string CoverBodyText = "#C7D0D9";
    public const string CoverFooterText = "#7E8B99";

    // Мягкие тинты статусных цветов — фон callout-блоков.
    public const string RiskBg = "#FBEAEA";
    public const string AttentionBg = "#FCF2E3";
    public const string StrengthBg = "#EAF5EF";
    public const string DataBg = "#F1F3F5";

    public const int ChartWidth = 1200;
    public const int ChartHeight = 400;
    public const int MiniChartWidth = 380;
    public const int MiniChartHeight = 220;

    public const string EmbeddedFontName = "DejaVu Sans";
    public const string EmbeddedFontRelativePath = "Assets/Fonts/DejaVuSans.ttf";

    public const int TableMaxRowsUnlimited = 0;
    public const int HorizontalBarRowHeightPx = 36;
    public const int HorizontalBarBaseHeightPx = 120;
    public const int DefaultMaxLabelLength = 48;
    public const int HierarchyIndentSpaces = 3;

    // Типографическая шкала.
    public const float FontCoverTitle = 28f;
    public const float FontCoverSubtitle = 13f;
    public const float FontH1 = 16f;
    public const float FontH2 = 12f;
    public const float FontBody = 9f;
    public const float FontCaption = 7.5f;
    public const float FontKpiValue = 15f;
    public const float FontKpiLabel = 8f;

    // Иконки-глифы (без внешних SVG — шрифт DejaVu Sans их поддерживает).
    public const string GlyphUp = "▲";
    public const string GlyphDown = "▼";
    public const string GlyphDot = "●";
    public const string GlyphWarning = "⚠";
    public const string GlyphCheck = "✓";

    /// <summary>Русская подпись типа отчёта — для бейджа хедера и обложки.</summary>
    public static string ReportKindLabel(PdfReportKind kind) => kind switch
    {
        PdfReportKind.Forecast => "ПРОГНОЗ",
        PdfReportKind.Fact => "ОЦЕНКА ФАКТА",
        PdfReportKind.Compare => "СРАВНЕНИЕ ВЕРСИЙ",
        PdfReportKind.Trend => "ТРЕНДОВЫЙ АНАЛИЗ",
        PdfReportKind.Combined => "СВОДНЫЙ ОТЧЁТ",
        _ => "ОТЧЁТ",
    };

    /// <summary>Цвет бейджа/акцента типа отчёта.</summary>
    public static string ReportKindColor(PdfReportKind kind) => kind switch
    {
        PdfReportKind.Forecast => AccentA,
        PdfReportKind.Fact => AccentB,
        PdfReportKind.Compare => Neutral,
        PdfReportKind.Trend => Positive,
        PdfReportKind.Combined => AccentDark,
        _ => AccentA,
    };

    /// <summary>Паритет client/src/utils/interpretationStyle.ts + русские enum-подписи.</summary>
    public static string InterpretationColor(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Muted;
        var lower = text.ToLowerInvariant();
        if (lower.Contains("высок") || lower.Contains("high") || lower.Contains("above")
            || lower.Contains("отлич") || lower.Contains("excellent") || lower.Contains("хорош") || lower.Contains("good"))
            return Positive;
        if (lower.Contains("низк") || lower.Contains("low") || lower.Contains("below")
            || lower.Contains("критич") || lower.Contains("critical")
            || lower.Contains("вниман") || lower.Contains("needsattention") || lower.Contains("needs attention"))
            return Negative;
        if (lower.Contains("средн") || lower.Contains("medium") || lower.Contains("moderate")
            || lower.Contains("norm") || lower.Contains("удовлетвор") || lower.Contains("satisfactory"))
            return Neutral;
        return Muted;
    }

    public static string DeltaColor(decimal delta) => delta >= 0 ? Negative : Positive;
}

/// <summary>Строка иерархической таблицы PDF (отступ по Level).</summary>
public sealed record HierarchicalPdfRow(int Level, string[] Cells, string? InterpretationText = null);
