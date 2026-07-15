namespace EngineeringPerformance.Application.Export;

/// <summary>Дизайн-токены PDF — паритет portal <c>wpColors</c> + interpretationStyle.</summary>
public static class ReportVisualTheme
{
    /// <summary>Как wpColors.heading / sidebar.</summary>
    public const string Primary = "#2C3E50";

    /// <summary>Как wpColors.sidebarHeader.</summary>
    public const string PrimaryDark = "#1A252F";

    /// <summary>Как wpColors.accent.</summary>
    public const string AccentA = "#3498DB";

    /// <summary>Как wpColors.accentDark.</summary>
    public const string AccentDark = "#2980B9";

    public const string AccentB = "#8E44AD";
    public const string Positive = "#27AE60";
    public const string Negative = "#E74C3C";
    public const string Neutral = "#F39C12";

    /// <summary>Как wpColors.muted.</summary>
    public const string Muted = "#95A5A6";

    /// <summary>Светлая карточка KPI (portal accentHover-ish).</summary>
    public const string CardBg = "#EBF5FB";

    public const string PageBg = "#F0F2F5";
    public const string HeaderText = "#ECF0F1";

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
    public const int HierarchyIndentSpaces = 2;

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
