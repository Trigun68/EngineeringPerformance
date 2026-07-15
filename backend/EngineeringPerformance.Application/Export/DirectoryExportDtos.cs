namespace EngineeringPerformance.Application.Export;

/// <summary>Запрос экспорта справочника: видимые колонки и строки из AG Grid.</summary>
public sealed record DirectoryExportRequest(
    string TabName,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string?>> Rows);
