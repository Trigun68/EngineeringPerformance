using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Import;

public sealed record ImportRowErrorDto(int RowNumber, string Message);

public sealed record DataImportResultDto(
    bool Success,
    int RecordsImported,
    string? ErrorMessage,
    int IterationId,
    string? SuccessDetails = null,
    IReadOnlyList<ImportRowErrorDto>? RowErrors = null);

public sealed record DataImportIterationInfoDto(
    DateTime ImportedAt,
    int RecordCount,
    string? Description,
    string? SourceFileName);

/// <summary>
/// Предпросмотр импорта трудоёмкости: типы документов из Excel, отсутствующие в справочнике.
/// </summary>
public sealed record LaborImportPreviewDto(
    IReadOnlyList<string> UnmatchedDocumentTypes,
    int TotalDataRows);

public sealed record ProjectImportRequest(
    DataImportType DataType,
    string? Description,
    bool AddMissingLaborToCatalog = false);
