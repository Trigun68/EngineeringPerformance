using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Registers;

public sealed record NormFormulaDto(
    FormulaType Type,
    IReadOnlyDictionary<string, decimal> Coefficients,
    decimal MinVolume,
    decimal MaxVolume,
    ExtrapolationMode Extrapolation,
    IReadOnlyList<PiecewisePointDto>? PiecewisePoints = null,
    IReadOnlyList<decimal>? PolynomialCoeffs = null);

public sealed record PiecewisePointDto(decimal Volume, decimal Norm);

public sealed record DeviationFormulaDto(
    DeviationModelType ModelType,
    IReadOnlyDictionary<string, decimal> Coefficients);

public sealed record IicNormDto(
    int Id,
    int WorkDirectionId,
    string? WorkDirectionName,
    string? WorkDirectionCode,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId,
    int? ResolvedUnitId,
    string? UnitName,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    NormFormulaDto? NormFormula = null,
    DeviationFormulaDto? DeviationFormula = null,
    DateTime? MigratedAt = null,
    int RecalculatedVersionCount = 0);

public sealed record IicNormHistoryItem(
    int? Id,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId,
    NormFormulaDto? NormFormula = null,
    DeviationFormulaDto? DeviationFormula = null);

public sealed record IicNormWriteRequest(
    int WorkDirectionId,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    NormFormulaDto? NormFormula = null,
    DeviationFormulaDto? DeviationFormula = null,
    IReadOnlyList<IicNormHistoryItem>? History = null);
