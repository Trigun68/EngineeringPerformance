namespace EngineeringPerformance.Application.Analytics;

public sealed record CompareChartPointDto(
    string Category,
    int? WorkDirectionId,
    decimal? ValueA,
    decimal? ValueB,
    decimal? Delta);

public sealed record CompareChartResultDto(
    string Level,
    string Metric,
    string VersionAName,
    string VersionBName,
    IReadOnlyList<CompareChartPointDto> Points);
