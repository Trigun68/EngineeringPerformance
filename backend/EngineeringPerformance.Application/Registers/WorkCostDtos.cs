using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Registers;

public sealed record WorkCostRowDto(
    int Id,
    int WorkDirectionId,
    int? ParentId,
    int SortOrder,
    string WorkDirectionName,
    string? WorkDirectionCode,
    decimal? RatePerHour,
    DateTime? EffectiveFrom,
    int? PeriodId);

public sealed record WorkCostBatchItem(int WorkDirectionId, decimal RatePerHour);

public sealed record WorkCostBatchRequest(
    DateTime EffectiveFrom,
    WorkCostLevel Level,
    IReadOnlyList<WorkCostBatchItem>? Items);

public sealed record WorkCostBatchResult(int WrittenCount);

public sealed record WorkCostPeriodDto(
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool EffectiveToVaries,
    int DirectionCount,
    decimal? UniformRate);

public sealed record WorkCostPeriodDeleteResult(int DeletedCount);
