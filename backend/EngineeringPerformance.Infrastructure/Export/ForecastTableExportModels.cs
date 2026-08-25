namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Общая модель табличного экспорта прогноза (PDF и Excel — один состав).</summary>
internal enum ForecastHourTone
{
    None,
    Optimistic,
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
    public const string OptimisticFillHex = "#27AE60";
    public const string PessimisticFillHex = "#E74C3C";
    public const string TotalFillHex = "#1E3A5F";
    public const string StripeFillHex = "#F8FAFC";
    public const string TitleColorHex = "#000000";
    public const float TitleFontSize = 13f;
    public const float TableFontSize = 8f;
    public const float RowHeightExcel = 18f;
}
