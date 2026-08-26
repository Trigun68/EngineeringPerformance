namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Тренд производительности нормы: N(V*) через существующий ComputeForecastHours движка,
/// без новых методов движка и без сравнения коэффициентов a/b разных FormulaType.
/// </summary>
public sealed record ProductivityTrendRequest(
    DateTime? PeriodStart = null,
    DateTime? PeriodEnd = null,
    int[]? WorkDirectionIds = null,
    int[]? ProjectIds = null,
    string VolumeMode = "median");

public sealed record ProductivityTrendPointDto(
    DateTime Date,
    double? HoursAtVStar,
    double? HoursPerUnit,
    double? Index,
    int? NormId,
    string FormulaType,
    string Flag);

public sealed record ProductivityTrendSegmentDto(
    int? NormId,
    string NormName,
    string FormulaType,
    string Color,
    string SegmentKey,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    IReadOnlyList<ProductivityTrendPointDto> Points);

public sealed record ProductivityTrendGroupDto(
    int GroupId,
    string GroupName,
    double? VStar,
    string VStarSource,
    IReadOnlyList<ProductivityTrendSegmentDto> Segments);

public sealed record ProductivityTrendLegendDto(
    string FormulaType,
    string DisplayName,
    string Color);

public sealed record ProductivityTrendTransitionDto(
    int GroupId,
    string GroupName,
    int OldNormId,
    int NewNormId,
    DateTime EffectiveFrom,
    string OldFormulaType,
    string NewFormulaType,
    double? VStarOld,
    double? VStarNew,
    double? HoursOldP10,
    double? HoursNewP10,
    double? HoursOldMedian,
    double? HoursNewMedian,
    double? HoursOldP90,
    double? HoursNewP90,
    double? StandardEffectHours,
    double? MixEffectHours,
    double WeightHours,
    string Verdict,
    bool CurvesCross,
    bool LogVolumeLtOne);

public sealed record ProductivityTrendCounterfactualDto(
    int GroupId,
    string GroupName,
    int ProjectId,
    string ProjectCode,
    double Volume,
    double? HoursOld,
    double? HoursNew,
    double? DeltaHours);

public sealed record ProductivityTrendResponse(
    string VolumeMode,
    ProductivityTrendGroupDto Overall,
    IReadOnlyList<ProductivityTrendGroupDto> ByGroup,
    IReadOnlyList<ProductivityTrendLegendDto> Legend,
    IReadOnlyList<ProductivityTrendTransitionDto> Transitions,
    IReadOnlyList<ProductivityTrendCounterfactualDto> Counterfactuals,
    IReadOnlyList<string> Warnings);

public interface IProductivityTrendAnalyticsService
{
    Task<ProductivityTrendResponse> GetProductivityTrendAsync(
        ProductivityTrendRequest request,
        CancellationToken cancellationToken = default);
}
