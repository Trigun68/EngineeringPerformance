namespace EngineeringPerformance.Application.Export;

/// <summary>Тон заливки столбца сценария — паритет грида на странице проекта.</summary>
public enum PdfScenarioTone
{
    None,
    Optimistic,
    Base,
    Pessimistic,
}

/// <summary>Описание колонки визуальной PDF-таблицы.</summary>
public sealed record PdfTableColumn(
    string Header,
    bool Numeric = false,
    PdfScenarioTone Tone = PdfScenarioTone.None,
    float RelativeWidth = 1f)
{
    public static PdfTableColumn Text(string header, float width = 1f) =>
        new(header, false, PdfScenarioTone.None, width);

    public static PdfTableColumn Number(string header, PdfScenarioTone tone = PdfScenarioTone.None, float width = 1f) =>
        new(header, true, tone, width);

    public static PdfTableColumn Optimistic(string? header = null, float width = 1f) =>
        Number(header ?? PdfScenarioLabels.Optimistic, PdfScenarioTone.Optimistic, width);

    public static PdfTableColumn BaseHours(string? header = null, float width = 1f) =>
        Number(header ?? PdfScenarioLabels.Base, PdfScenarioTone.Base, width);

    public static PdfTableColumn Pessimistic(string? header = null, float width = 1f) =>
        Number(header ?? PdfScenarioLabels.Pessimistic, PdfScenarioTone.Pessimistic, width);
}
