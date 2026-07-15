namespace EngineeringPerformance.Application.Analytics;

public sealed record GroupAnalysisProjectRowDto(
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    int? VersionId,
    string? VersionName,
    decimal? TotalFactLaborHours,
    decimal? WeightedZScore,
    string? WeightedZInterpretation);

/// <summary>Агрегация показателей факта по проектам одной программы/заказчика.</summary>
public sealed record GroupAnalysisResultDto(
    string GroupCriterion,
    string? GroupValue,
    IReadOnlyList<GroupAnalysisProjectRowDto> Projects,
    decimal? AggregateWeightedZScore,
    string? AggregateWeightedZInterpretation);

/// <summary>Метаданные для вкладки «Тренд по группам» — фактический тренд на /analytics/trends.</summary>
public sealed record GroupTrendInfoDto(
    string Message,
    string TrendsPagePath,
    int PeerProjectCount);
