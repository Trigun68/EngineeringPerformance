<<<<<<< HEAD
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводка «план/факт» для вкладки «Отчёт»: план = основная версия Fact (её PlannedHours) либо,
/// если основной Fact-версии нет, основная версия Plan; факт = основная версия Fact (её
/// ActualHours), либо <c>null</c>, если основная версия Fact не назначена.
=======
namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Сводка «план/факт» для вкладки «Отчёт»: план = основная версия Fact (её сценарные часы) либо,
/// если основной Fact-версии нет, основная версия Plan; факт = основная версия Fact
/// (allocated PlannedHours), либо <c>null</c>, если версий Fact нет.
/// Семантика часов и выбора версий идентична <c>PerformanceReportService</c>.
>>>>>>> origin/master
/// </summary>
public sealed record ProjectReportSummaryDto(
    int? PlanVersionId,
    string? PlanVersionName,
    decimal? PlanHours,
    decimal? PlanWeightedZScore,
    string? PlanPercentile,
<<<<<<< HEAD
    PerformanceInterpretation? PlanInterpretation,
=======
    string? PlanInterpretation,
>>>>>>> origin/master
    int? FactVersionId,
    string? FactVersionName,
    decimal? FactHours,
    decimal? FactWeightedZScore,
    string? FactPercentile,
<<<<<<< HEAD
    PerformanceInterpretation? FactInterpretation);
=======
    string? FactInterpretation);
>>>>>>> origin/master
