using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>Агрегированный отчёт производительности проекта (план + факт).</summary>
public sealed record PerformanceReportDto(
    PerformanceReportProjectDto Project,
    PerformanceReportVersionBlockDto? Plan,
    PerformanceReportVersionBlockDto? Fact,
    IReadOnlyList<PerformanceReportVersionHistoryPointDto> VersionHistory,
    PerformanceReportAccuracyDto Accuracy,
    PerformanceReportInsightsDto Insights,
    IReadOnlyList<PerformanceReportMergedDirectionRowDto> Directions,
    int NormSourcesUsed);

public sealed record PerformanceReportProjectDto(
    int Id,
    string Code,
    string Name,
    string? Gip,
    string? Customer,
    string? WorkStatus,
    string? Type,
    int PlanVersionCount,
    int FactVersionCount);

public sealed record PerformanceReportVersionBlockDto(
    int VersionId,
    string VersionName,
    DateTime CreatedAt,
    AnalysisDirection Direction,
    decimal? WeightedZScore,
    string WeightedZScorePercentile,
    string? WeightedZScoreInterpretation,
    decimal? TotalLaborHours,
    decimal? UnallocatedLaborHours,
    IReadOnlyList<PhysicalVolumeTotalByUnitDto> PhysicalVolumeTotalsByUnit,
    IReadOnlyList<PerformanceReportDirectionRowDto> Directions);

/// <summary>Строка направления одной версии (сырой блок plan/fact).</summary>
public sealed record PerformanceReportDirectionRowDto(
    int WorkDirectionId,
    int? ParentId,
    int Level,
    string Code,
    string Name,
    string? UnitName,
    decimal? FactPhysicalVolume,
    decimal? FactLaborHours,
    decimal? SubtreeHours,
    decimal? FactProductivity,
    decimal? NormProductivity,
    decimal? NormStdDev,
    decimal? ZScore,
    string? Interpretation);

/// <summary>Merged-строка дерева «Направление»: объём факт←план, dual труд/произв/Z.</summary>
public sealed record PerformanceReportMergedDirectionRowDto(
    int WorkDirectionId,
    int? ParentId,
    int Level,
    string Code,
    string Name,
    string? UnitName,
    decimal? PhysicalVolume,
    decimal? PlanLaborHours,
    decimal? FactLaborHours,
    decimal? PlanSubtreeHours,
    decimal? FactSubtreeHours,
    decimal? PlanProductivity,
    decimal? FactProductivity,
    decimal? NormProductivity,
    decimal? NormStdDev,
    decimal? PlanZScore,
    decimal? FactZScore,
    string? PlanInterpretation,
    string? FactInterpretation);

public sealed record PerformanceReportVersionHistoryPointDto(
    int VersionId,
    string VersionName,
    DateTime CreatedAt,
    AnalysisDirection Direction,
    decimal? WeightedZScore,
    decimal? TotalLaborHours);

public sealed record PerformanceReportAccuracyDto(
    decimal? AveragePercent,
    IReadOnlyList<PerformanceReportAccuracyItemDto> Items);

public sealed record PerformanceReportAccuracyItemDto(
    string CharacteristicName,
    string? OptionName,
    decimal? ValuePercent);

public sealed record PerformanceReportInsightsDto(
    IReadOnlyList<PerformanceReportInsightItemDto> Items,
    IReadOnlyList<PerformanceReportKeyDisciplineDto> TopNegative,
    IReadOnlyList<PerformanceReportKeyDisciplineDto> TopPositive);

public sealed record PerformanceReportInsightItemDto(
    string Kind,
    string Title,
    string Detail);

public sealed record PerformanceReportKeyDisciplineDto(
    int WorkDirectionId,
    string Name,
    string Code,
    decimal? ZScore,
    decimal SubtreeHours,
    decimal InfluenceScore,
    decimal? PhysicalVolume);
