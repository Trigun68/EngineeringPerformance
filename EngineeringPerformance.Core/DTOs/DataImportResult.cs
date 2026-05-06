namespace EngineeringPerformance.Core.DTOs;

public sealed record DataImportResult(
    bool Success,
    int RecordsImported,
    string? ErrorMessage,
    int IterationId,
    string? SuccessDetails = null);
