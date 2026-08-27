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
    IReadOnlyList<string> Warnings);

public interface IProjectTrendService
{
    Task<ProjectTrendDto> GetAsync(int projectId, CancellationToken cancellationToken = default);
}
