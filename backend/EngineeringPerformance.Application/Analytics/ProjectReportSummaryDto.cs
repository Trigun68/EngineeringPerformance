namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводка «план/факт» для вкладки «Отчёт»: план = основная версия Fact (её сценарные часы) либо,
/// если основной Fact-версии нет, основная версия Plan; факт = основная версия Fact
/// (allocated PlannedHours), либо <c>null</c>, если версий Fact нет.
/// Семантика часов и выбора версий идентична <c>PerformanceReportService</c>.
/// <see cref="PlanSourceDirection"/> — <c>AnalysisDirection</c> версии-источника плана (часто Fact).
/// </summary>
public sealed record ProjectReportSummaryDto(
    int? PlanVersionId,
    string? PlanVersionName,
    int? PlanSourceDirection,
    bool PlanIsPrimary,
    decimal? PlanHours,
    decimal? PlanWeightedZScore,
    string? PlanPercentile,
    string? PlanInterpretation,
    int? FactVersionId,
    string? FactVersionName,
    bool FactIsPrimary,
    decimal? FactHours,
    decimal? FactWeightedZScore,
    string? FactPercentile,
    string? FactInterpretation);
