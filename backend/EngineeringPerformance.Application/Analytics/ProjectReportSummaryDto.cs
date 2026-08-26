using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводка «план/факт» для вкладки «Отчёт»: план = основная версия Fact (её PlannedHours) либо,
/// если основной Fact-версии нет, основная версия Plan; факт = основная версия Fact (её
/// ActualHours), либо <c>null</c>, если основная версия Fact не назначена.
/// </summary>
public sealed record ProjectReportSummaryDto(
    int? PlanVersionId,
    string? PlanVersionName,
    decimal? PlanHours,
    decimal? PlanWeightedZScore,
    string? PlanPercentile,
    PerformanceInterpretation? PlanInterpretation,
    int? FactVersionId,
    string? FactVersionName,
    decimal? FactHours,
    decimal? FactWeightedZScore,
    string? FactPercentile,
    PerformanceInterpretation? FactInterpretation);
