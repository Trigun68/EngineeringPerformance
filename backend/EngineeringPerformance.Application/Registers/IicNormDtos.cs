namespace EngineeringPerformance.Application.Registers;

public sealed record IicNormDto(
    int Id,
    int WorkDirectionId,
    string? WorkDirectionName,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record IicNormHistoryItem(
    int? Id,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId);

public sealed record IicNormWriteRequest(
    int WorkDirectionId,
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    int? UnitId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    IReadOnlyList<IicNormHistoryItem>? History = null);
