using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>Один блок (план или факт) сводки «план/факт» — см. <see cref="ProjectReportSummaryDto"/>.</summary>
public sealed record ProjectReportSummaryBlock(
    decimal? Hours,
    decimal? WeightedZScore,
    string Percentile,
    PerformanceInterpretation? Interpretation);

/// <summary>
/// Расчёт одного блока (план или факт) сводки «план/факт» из строк производительности —
/// та же математика, что и <see cref="ProjectVersionAnalysisTotalsCalculator"/>, но без
/// физ. объёмов/нераспределённых часов (не нужны на вкладке «Отчёт» для этой сводки).
/// </summary>
public static class ProjectReportSummaryCalculator
{
    public static ProjectReportSummaryBlock Calculate(IReadOnlyList<PerformanceRowDto> performanceRows, decimal? totalHours)
    {
        var weightedZ = WeightedZScoreCalculator.FromRootPerformanceRows(
            performanceRows,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);

        return new ProjectReportSummaryBlock(
            Hours: totalHours,
            WeightedZScore: weightedZ,
            Percentile: PerformancePercentileHelper.GetPercentile(weightedZ),
            Interpretation: PerformanceInterpretationHelper.GetInterpretation(weightedZ));
    }
}
