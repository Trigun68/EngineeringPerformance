namespace EngineeringPerformance.Application.Analytics;

public sealed record TrendPointDto(
    DateTime Timestamp,
    int VersionId,
    string VersionName,
    string Metric,
    decimal? Value,
    string? Label = null);

public sealed record TrendEventDto(
    DateTime Timestamp,
    string Type,
    string Label,
    int? VersionId = null);

public sealed record TrendSeriesDto(
    string Metric,
    string DisplayName,
    IReadOnlyList<TrendPointDto> Points);

public sealed record TrendAnalyticsResultDto(
    string Granularity,
    AssessmentFormulaDescriptor Formula,
    IReadOnlyList<TrendSeriesDto> Series,
    IReadOnlyList<TrendEventDto> Events);

public sealed record TrendDetailRowDto(
    DateTime Timestamp,
    int VersionId,
    string FacilityCode,
    string FacilityName,
    string PvCode,
    string PvName,
    decimal? FactLaborHours,
    decimal? ZScore,
    string Interpretation);

public interface ITrendAnalyticsService
{
    Task<TrendAnalyticsResultDto> GetTrendsAsync(
        int projectId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string granularity,
        string formulaId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<AssessmentFormulaDescriptor> ListFormulas();

    Task<IReadOnlyList<TrendDetailRowDto>> GetDetailAsync(
        int projectId,
        int versionId,
        string formulaId,
        CancellationToken cancellationToken = default);
}
