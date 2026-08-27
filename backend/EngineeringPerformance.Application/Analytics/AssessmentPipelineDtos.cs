using System.Text.Json;

namespace EngineeringPerformance.Application.Analytics;

public sealed record AssessmentParameterDescriptor(
    string Key,
    string Type,
    string Label,
    string? DefaultValue = null);

public sealed record AssessmentFormulaDescriptor(
    string Id,
    string DisplayName,
    string Description,
    IReadOnlyList<AssessmentParameterDescriptor> Parameters);

public sealed record AssessmentRequest(
    int ProjectId,
    int VersionId,
    string FormulaId = DocSetProportionalFormula.Id,
    JsonElement? FormulaParams = null);

public sealed record AssessmentSummaryDto(
    decimal? TotalFactLaborHours,
    decimal? WeightedZScore);

public sealed record AssessmentResultDto(
    string FormulaId,
    IReadOnlyList<PerformanceRowDto> Performance,
    AssessmentSummaryDto Summary);

/// <summary>
/// Результат пакетной оценки версий: успешные по VersionId и ошибки по VersionId.
/// Версия с ошибкой не срывает остальные (портфельные выборки продолжают работать).
/// </summary>
public sealed record BatchAssessmentResult(
    IReadOnlyDictionary<int, AssessmentResultDto> Results,
    IReadOnlyDictionary<int, string> Errors);

public static class DocSetProportionalFormula
{
    public const string Id = "v1-docset-proportional";
    public const string DisplayName = "Пропорциональное распределение LI по DocSet";
}
