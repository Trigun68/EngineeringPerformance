namespace EngineeringPerformance.Core.DTOs;

public sealed record ProjectAnalysisReportDto(
    string ProjectName,
    string ProjectCode,
    string VersionName,
    decimal? PermIndicator,
    string Interpretation,
    string Percentile,
    string Status,
    decimal TotalLaborHours,
    IReadOnlyList<DisciplineAnalysisDto> Disciplines,
    decimal? AccuracyScore,
    string AccuracyInterpretation,
    IReadOnlyList<AccuracyCharacteristicDto> AccuracyCharacteristics,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> KeyDisciplines);

/// <summary>
/// Характеристика проекта, входящая в расчёт «Точности сравнения»
/// (средней PD% по всем выбранным опциям).
/// </summary>
public sealed record AccuracyCharacteristicDto(
    string CharacteristicName,
    string OptionName,
    decimal? ValuePercent);
