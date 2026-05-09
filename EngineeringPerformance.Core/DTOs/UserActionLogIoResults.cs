namespace EngineeringPerformance.Core.DTOs;

// Результат экспорта журнала в файл с последующим удалением записей из БД.
public sealed record UserActionLogExportResult(
    string FilePath,
    int ExportedCount,
    int DeletedCount);

// Результат импорта журнала из файла. Дубликаты и orphan-записи (UserId
// отсутствует в БД) не вставляются — попадают в SkippedReasons.
public sealed record UserActionLogImportResult(
    int TotalRows,
    int ImportedCount,
    int SkippedDuplicates,
    int SkippedOrphans,
    int SkippedInvalid,
    IReadOnlyList<string> Errors);
