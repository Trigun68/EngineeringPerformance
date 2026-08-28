namespace EngineeringPerformance.Application.Analytics;

public sealed record ZTrendRequest(
    DateTime? PeriodStart = null,
    DateTime? PeriodEnd = null,
    int[]? WorkDirectionIds = null,
    int[]? ProjectIds = null,
    string WeightMode = "basisHours");

public sealed record ZTrendPointDto(
    DateTime Date,
    double? ZScore,
    double Weight,
    int ProjectCount,
    int? NormId,
    string Flag,
    IReadOnlyList<ZTrendContributionDto>? Contributions = null);

public sealed record ZTrendContributionDto(
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    int VersionId,
    string VersionName,
    double? ZScore,
    double Weight);

public sealed record ZTrendSegmentDto(
    int? NormId,
    string NormName,
    int? NormGroupId,
    string NormGroupName,
    string Color,
    string SegmentKey,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    IReadOnlyList<ZTrendPointDto> Points);

public sealed record ZTrendSeriesDto(
    string SeriesType,
    IReadOnlyList<ZTrendSegmentDto> Segments);

public sealed record ZTrendGroupDto(
    int GroupId,
    string GroupName,
    ZTrendSeriesDto Plan,
    ZTrendSeriesDto Fact);

public sealed record ZTrendLegendDto(
    int? NormId,
    string NormName,
    int? GroupId,
    string GroupName,
    string Color,
    string SegmentKey,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);

public sealed record ZTrendFiltersDto(
    IReadOnlyList<ZTrendFilterProjectDto> Projects,
    IReadOnlyList<ZTrendFilterGroupDto> Groups,
    IReadOnlyList<string> WeightModes);

public sealed record ZTrendFilterProjectDto(int Id, string Code, string Name);

public sealed record ZTrendFilterGroupDto(int Id, string Code, string Name);

public sealed record ZTrendResponse(
    ZTrendGroupDto Overall,
    IReadOnlyList<ZTrendGroupDto> ByGroup,
    IReadOnlyList<ZTrendLegendDto> Legend,
    IReadOnlyList<string> Warnings,
    TrendDetailAggregationDto? Detail = null,
    HoursBridgeResultDto? DisciplineBridge = null,
    IReadOnlyList<GroupZeroBaselineDto>? GroupZeroBars = null,
    IReadOnlyList<CumulativeHoursPointDto>? PortfolioSCurve = null,
    HoursCoverageDto? Coverage = null,
    IReadOnlyList<MoverRowDto>? Movers = null,
    IReadOnlyList<PercentileBandPointDto>? ZPercentileBand = null,
    IReadOnlyList<TrendEventDto>? Events = null,
    IReadOnlyList<CumulativeHoursPointDto>? PlanSCurve = null,
    IReadOnlyList<NamedSCurveDto>? ProjectSCurves = null,
    decimal? RawHoursDelta = null);

/// <summary>Сырая точка версии до агрегации/сегментации (вход TrendEngine).</summary>
public sealed record ZTrendRawPoint(
    DateTime Date,
    int VersionId,
    int ProjectId,
    decimal? ZScore,
    decimal Weight,
    string SegmentKey,
    int? NormId,
    string NormName,
    int? GroupId,
    string GroupName,
    string Flag,
    int ProjectCount = 1,
    string ProjectCode = "",
    string ProjectName = "",
    string VersionName = "",
    IReadOnlyList<ZTrendContributionDto>? Contributions = null);

public interface IZTrendEngine
{
    decimal? ClipZ(decimal? z, double zClipMax, out string flag);

    IReadOnlyList<ZTrendRawPoint> AggregateSameDay(
        IReadOnlyList<ZTrendRawPoint> points,
        string weightMode);

    ZTrendSeriesDto BuildSeries(
        IReadOnlyList<ZTrendRawPoint> points,
        string seriesType,
        AnalyticsTrendsOptions options,
        IReadOnlyDictionary<string, string>? colorBySegmentKey = null);

    string LightenColor(string hexColor, double lightenStep, int shadeIndex);

    string ColorForSegment(int paletteIndex, int shadeIndex, double lightenStep);
}

public interface IZTrendAnalyticsService
{
    Task<ZTrendResponse> GetZTrendsAsync(ZTrendRequest request, CancellationToken cancellationToken = default);

    Task<ZTrendFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default);
}
