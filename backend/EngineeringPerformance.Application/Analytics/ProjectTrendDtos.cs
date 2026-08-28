namespace EngineeringPerformance.Application.Analytics;

public sealed record ProjectTrendZPointDto(
    DateTime Date,
    string Direction,
    string VersionName,
    decimal? ZScore,
    decimal Hours);

public sealed record ProjectTrendDto(
    string ProjectCode,
    string ProjectName,
    int? FactVersionId,
    string? FactVersionName,
    DateTime? FactVersionDate,
    int? PlanVersionId,
    string? PlanVersionName,
    IReadOnlyList<ProjectTrendZPointDto> ZHistory,
    HoursBridgeResultDto HoursBridge,
    IReadOnlyList<ZeroBaselineBarDto> ZFromZero,
    IReadOnlyList<CumulativeHoursPointDto> SCurve,
    decimal? DeltaWaterfallStart,
    IReadOnlyList<WaterfallStepDto>? DeltaWaterfall,
    IReadOnlyList<string> Warnings,
    HoursCoverageDto Coverage,
    IReadOnlyList<ProjectTrendZPointDto> PortfolioZHistory,
    IReadOnlyList<PercentileBandPointDto> ZPercentileBand,
    IReadOnlyList<TrendEventDto> Events,
    IReadOnlyList<CumulativeHoursPointDto> PlanSCurve,
    decimal? RawHoursDelta,
    decimal? PlanDeltaWaterfallStart,
    IReadOnlyList<WaterfallStepDto>? PlanDeltaWaterfall);

public interface IProjectTrendService
{
    Task<ProjectTrendDto> GetAsync(int projectId, CancellationToken cancellationToken = default);
}
