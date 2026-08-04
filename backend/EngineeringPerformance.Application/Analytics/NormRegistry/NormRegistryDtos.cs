using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics.NormRegistry;

public static class NormDerivedStatus
{
    public const string Active = "Active";
    public const string Historical = "Historical";
    public const string Future = "Future";

    public static string Resolve(DateTime effectiveFrom, DateTime? effectiveTo, DateTime today)
    {
        var from = effectiveFrom.Date;
        var to = effectiveTo?.Date;
        var d = today.Date;
        if (from > d) return Future;
        if (to is not null && to.Value < d) return Historical;
        return Active;
    }

    public static bool Covers(DateTime effectiveFrom, DateTime? effectiveTo, DateTime day)
    {
        var d = day.Date;
        if (effectiveFrom.Date > d) return false;
        if (effectiveTo is { } to && to.Date < d) return false;
        return true;
    }

    /// <summary>Пересечение полуинтервалов [from, to] где to=null → +∞ (включительно по датам).</summary>
    public static bool PeriodsOverlap(
        DateTime aFrom, DateTime? aTo,
        DateTime bFrom, DateTime? bTo)
    {
        var aEnd = aTo?.Date ?? DateTime.MaxValue.Date;
        var bEnd = bTo?.Date ?? DateTime.MaxValue.Date;
        var start = aFrom.Date > bFrom.Date ? aFrom.Date : bFrom.Date;
        var end = aEnd < bEnd ? aEnd : bEnd;
        return start <= end;
    }
}

public sealed record NormHealthNormInput(
    int Id,
    int WorkDirectionId,
    string GroupName,
    string GroupCode,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    FormulaType FormulaType,
    IReadOnlyDictionary<string, decimal> Coefficients,
    decimal? StandardDeviation,
    DeviationModelType DeviationModelType,
    IReadOnlyDictionary<string, decimal> DeviationCoefficients,
    DateTime CreatedAt);

public sealed record NormUsageHint(
    int NormId,
    int TotalVersions,
    DateTime? LastVersionDate);

public sealed record NormHealthIssueDto(
    Guid Id,
    string RuleCode,
    string Severity,
    IReadOnlyList<int> NormIds,
    int? GroupId,
    string Message,
    object? Details,
    DateTime DetectedAt);

public sealed record HealthSummaryDto(
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    IReadOnlyList<NormHealthIssueDto> Issues);

public sealed record NormFilterDto(
    int[]? GroupIds = null,
    string[]? Statuses = null,
    string[]? FormulaTypes = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    bool? OnlyWithIssues = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDesc = false);

public sealed record NormListItemDto(
    int Id,
    int GroupId,
    string GroupName,
    string? GroupCode,
    string FormulaType,
    string FormulaDisplay,
    IReadOnlyDictionary<string, decimal> Parameters,
    double? MeanPerformance,
    double? StdDeviation,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string Status,
    int TotalVersions,
    int IssueCount,
    string? MaxIssueSeverity);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record NormUsageStatsDto(
    int TotalVersions,
    int PlanVersions,
    int FactVersions,
    int PrimaryVersions,
    DateTime? FirstVersionDate,
    DateTime? LastVersionDate,
    IReadOnlyList<MonthlyVersionCountDto> MonthlyHistogram);

public sealed record MonthlyVersionCountDto(string Month, int Count);

public sealed record NormChangeAuditDto(
    long Id,
    string Action,
    string ChangedBy,
    DateTime Timestamp,
    string? OldValue,
    string? NewValue);

public sealed record NormDetailsDto(
    NormListItemDto Norm,
    NormUsageStatsDto Usage,
    int? PreviousNormId,
    int? NextNormId,
    IReadOnlyList<NormChangeAuditDto> AuditHistory,
    IReadOnlyList<NormHealthIssueDto> Issues,
    string? UnitName,
    string? CoefficientsJson,
    string? DeviationCoefficientsJson,
    string DeviationModelType);

public sealed record TimelineBarDto(
    int NormId,
    int GroupId,
    string GroupName,
    string Label,
    string Color,
    string Status,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool HasIssue);

public sealed record TimelineResponseDto(
    IReadOnlyList<TimelineBarDto> Bars,
    IReadOnlyList<TimelineGroupDto> Groups);

public sealed record TimelineGroupDto(int Id, string Name, string Color);

public sealed record NormRegistryFiltersMetaDto(
    IReadOnlyList<NormRegistryGroupOptionDto> Groups,
    IReadOnlyList<string> FormulaTypes,
    IReadOnlyList<string> Statuses);

public sealed record NormRegistryGroupOptionDto(int Id, string Name, string? Code);
