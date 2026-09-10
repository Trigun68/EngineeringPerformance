using EngineeringPerformance.Application.Export;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Общая модель табличного экспорта прогноза (PDF и Excel — один состав).</summary>
internal enum ForecastHourTone
{
    None,
    Optimistic,
    Base,
    Pessimistic,
}

internal sealed record ForecastTableColumn(
    string Header,
    float RelativeWidth,
    bool Numeric = false,
    ForecastHourTone Tone = ForecastHourTone.None);

internal sealed record ForecastTableCell(
    string Text,
    decimal? Number = null,
    ForecastHourTone Tone = ForecastHourTone.None,
    bool Bold = false);

internal sealed record ForecastTableRow(
    IReadOnlyList<ForecastTableCell> Cells,
    bool IsTotal = false);

internal sealed record ForecastTableSection(
    string? Title,
    IReadOnlyList<ForecastTableColumn> Columns,
    IReadOnlyList<ForecastTableRow> Rows);

internal sealed record ForecastTableDocument(
    string Title,
    string SheetName,
    bool Landscape,
    IReadOnlyList<ForecastTableSection> Sections,
    IReadOnlyList<string>? HeaderLines = null);

internal static class ForecastTableTheme
{
    public const string HeaderFillHex = "#2C3E50";
    // Пастель грида / отчётных форм — не насыщенные столбики графиков.
    public const string OptimisticFillHex = ReportVisualTheme.ScenarioOptimisticFill;
    public const string BaseFillHex = ReportVisualTheme.ScenarioBaseFill;
    public const string PessimisticFillHex = ReportVisualTheme.ScenarioPessimisticFill;
    public const string TotalFillHex = "#1E3A5F";
    public const string StripeFillHex = "#F8FAFC";
    public const string TitleColorHex = "#000000";
    public const string ContentTextHex = ReportVisualTheme.Primary;
    public const float TitleFontSize = 13f;
    public const float TableFontSize = 8f;
    public const float RowHeightExcel = 18f;
    public const float HeaderRowHeightExcel = 28f;
    public const double ExcelSheetWidth = 170d;
    public const double ExcelMinNumericWidth = 14d;
    public const double ExcelMinCodeWidth = 12d;
    public const double ExcelMinNameWidth = 36d;
    public const double ExcelMaxColumnWidth = 48d;
}
