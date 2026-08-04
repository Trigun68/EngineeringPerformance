using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Application.Registers;

namespace EngineeringPerformance.Application.Analytics.FactNorm;

public sealed record FactNormAnalysisRequest(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    int[]? ProjectIds = null,
    int[]? WorkDirectionIds = null);

public sealed record AccuracyDto(
    string Level,
    int DataPointsCount,
    int PeriodLengthDays,
    string? Hint);

public sealed record ChartPointDto(double X, double Y);

public sealed record NormComparisonDto(
    int NormId,
    int WorkDirectionId,
    string GroupName,
    string CurrentFormula,
    string ProposedFormula,
    double DeltaMean,
    double DeltaSigma,
    double CurrentR2,
    double ProposedR2,
    double DeltaR2,
    string Verdict,
    bool RequiresAttention,
    bool InsufficientData,
    bool SigmaZero,
    int? SuggestionId,
    IReadOnlyList<ChartPointDto>? ChartPoints = null,
    IReadOnlyDictionary<string, double>? ProposedParameters = null,
    string? Warning = null);

public sealed record NormSuggestionDto(
    int Id,
    int NormId,
    int WorkDirectionId,
    string GroupName,
    string SuggestionType,
    string Status,
    string ProposedFormulaType,
    Dictionary<string, double> ProposedParameters,
    double ProposedStdDeviation,
    double ProposedMeanPerformance,
    double CurrentR2,
    double ProposedR2,
    double DeltaR2,
    string Confidence,
    int DataPointsCount,
    DateTime PeriodStart,
    DateTime PeriodEnd);

public sealed record FactNormAnalysisResponse(
    AccuracyDto Accuracy,
    List<NormComparisonDto> Comparisons,
    List<NormSuggestionDto> Suggestions,
    List<string> Warnings,
    bool FromCache = false);

public sealed record ReferenceFormulaDto(
    string Code,
    string DisplayName,
    string Formula,
    string[] ParamNames,
    string Description);

public sealed record CreateNormFromSuggestionRequest(
    int SuggestionId,
    DateTime EffectiveFrom,
    string? FormulaType = null,
    Dictionary<string, double>? Parameters = null,
    double? MeanPerformance = null,
    double? StdDeviation = null);

public interface IFactNormAnalysisService
{
    Task<FactNormAnalysisResponse> AnalyzeAsync(
        FactNormAnalysisRequest request,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ReferenceFormulaDto> ListReferenceFormulas();

    Task<IicNormDto> CreateNormFromSuggestionAsync(
        CreateNormFromSuggestionRequest request,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(
        FactNormAnalysisRequest request,
        string format,
        CancellationToken cancellationToken = default);

    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
}
