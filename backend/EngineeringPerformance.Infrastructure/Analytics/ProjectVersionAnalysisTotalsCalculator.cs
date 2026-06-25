using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Расчёт итогов отчёта «Анализ версии проекта» из строк производительности и трудоёмкости.
/// </summary>
public static class ProjectVersionAnalysisTotalsCalculator
{
    public static ProjectVersionAnalysisSummaryDto Calculate(
        IReadOnlyList<PerformanceRowDto> performanceRows,
        decimal totalPlannedHours,
        decimal unallocatedPlannedHours)
    {
        var roots = performanceRows
            .Where(r => r.WorkDirectionId >= 0 && r.ParentId == null)
            .ToList();

        var weightedRoots = roots
            .Where(r => r.ZScore.HasValue && r.SubtreeHours is > 0)
            .ToList();

        decimal? weightedZ = null;
        if (weightedRoots.Count > 0)
        {
            var denominator = weightedRoots.Sum(r => r.SubtreeHours!.Value);
            if (denominator > 0)
            {
                weightedZ = Math.Round(
                    weightedRoots.Sum(r => r.ZScore!.Value * r.SubtreeHours!.Value) / denominator,
                    2);
            }
        }

        var physicalVolumeTotalsByUnit = performanceRows
            .Where(r => r.WorkDirectionId >= 0
                        && r.FactPhysicalVolume is > 0
                        && !string.IsNullOrWhiteSpace(r.NormUnitName))
            .GroupBy(r => r.NormUnitName!.Trim())
            .Select(g => new PhysicalVolumeTotalByUnitDto(
                g.Key,
                Math.Round(g.Sum(x => x.FactPhysicalVolume!.Value), 2)))
            .OrderBy(x => x.UnitName, StringComparer.Ordinal)
            .ToList();

        return new ProjectVersionAnalysisSummaryDto(
            TotalFactLaborHours: totalPlannedHours > 0 ? Math.Round(totalPlannedHours, 2) : null,
            WeightedZScore: weightedZ,
            WeightedZScorePercentile: PerformancePercentileHelper.GetPercentile(weightedZ),
            WeightedZScoreInterpretation: PerformanceInterpretationHelper.GetInterpretation(weightedZ),
            PhysicalVolumeTotalsByUnit: physicalVolumeTotalsByUnit,
            UnallocatedLaborHours: unallocatedPlannedHours > 0
                ? Math.Round(unallocatedPlannedHours, 2)
                : null);
    }
}
