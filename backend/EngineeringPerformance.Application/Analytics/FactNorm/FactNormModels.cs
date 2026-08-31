namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>Коды референтных формул для подгонки (не путать с FormulaType регистра).</summary>
public static class ReferenceFormulaCodes
{
    public const string Constant = "CONSTANT";
    public const string Linear = "LINEAR";
    public const string Power = "POWER";
    public const string Logarithmic = "LOGARITHMIC";
    public const string Exponential = "EXPONENTIAL";
    public const string Polynomial2 = "POLYNOMIAL2";
    public const string Hyperbolic = "HYPERBOLIC";
    public const string KimCurve = "KIMCURVE";

    public static readonly IReadOnlyList<string> All =
    [
        Constant, Linear, Power, Logarithmic, Exponential, Polynomial2, Hyperbolic,
    ];
}

/// <summary>Точка факта (x=объём, y=производительность). Метаданные проекта/версии для UI.</summary>
public readonly record struct FactPoint(
    double X,
    double Y,
    int ProjectId = 0,
    string? ProjectCode = null,
    string? ProjectName = null,
    int VersionId = 0,
    string? VersionName = null);

public sealed record FitMetrics(
    double R2,
    double AdjustedR2,
    double Aic,
    double Bic,
    double Mape,
    double Rmse,
    double Rss,
    int ParameterCount,
    int PointCount);

public sealed record FormulaFitResult(
    string Code,
    IReadOnlyDictionary<string, double> Parameters,
    FitMetrics Metrics,
    bool Success,
    string? FailureReason = null);

public enum NormVerdict
{
    ChangeFormulaType = 1,
    AdjustParameters = 2,
    NormIsActual = 3,
    InsufficientData = 4,
}

public enum AccuracyLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
}

public enum SuggestionConfidence
{
    Low = 1,
    Medium = 2,
    High = 3,
}

public sealed record AccuracyResult(
    AccuracyLevel Level,
    int DataPointsCount,
    int PeriodLengthDays,
    string? Hint);

public sealed record CurrentNormFitContext(
    string FormulaCode,
    IReadOnlyDictionary<string, double> Parameters,
    double MeanPerformance,
    double StdDeviation,
    double? CurrentR2);

public sealed record NormGroupComparisonInput(
    int NormId,
    int WorkDirectionId,
    string GroupName,
    CurrentNormFitContext CurrentNorm,
    IReadOnlyList<FactPoint> Points);

public sealed record NormSuggestionDraft(
    NormVerdict SuggestionType,
    string ProposedFormulaType,
    IReadOnlyDictionary<string, double> ProposedParameters,
    double ProposedStdDeviation,
    double ProposedMeanPerformance,
    double CurrentR2,
    double ProposedR2,
    double DeltaR2,
    SuggestionConfidence Confidence,
    int DataPointsCount);

public sealed record NormGroupComparisonResult(
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
    NormVerdict Verdict,
    bool RequiresAttention,
    bool SigmaZero,
    bool InsufficientData,
    int DataPointsCount,
    IReadOnlyList<FactPoint> ChartPoints,
    FormulaFitResult? BestFit,
    NormSuggestionDraft? Suggestion,
    string? Warning,
    double NormMeanPerformance,
    double FactMeanPerformance,
    double FactStdDeviation,
    double NormStdDeviation,
    IReadOnlyDictionary<string, double> CurrentParameters);

public sealed record FactNormEngineResult(
    AccuracyResult Accuracy,
    IReadOnlyList<NormGroupComparisonResult> Comparisons,
    IReadOnlyList<string> Warnings);
