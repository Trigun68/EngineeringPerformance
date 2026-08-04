using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Единый расчёт средневзвешенного Z по корневым направлениям.
/// Реализация для IPerformanceCalculationEngine.ComputeWeightedZFromPerformanceRows;
/// Infrastructure вызывает только через движок.
/// </summary>
public static class WeightedZScoreCalculator
{
    public static decimal? FromRootPerformanceRows(IReadOnlyList<PerformanceRowDto> performanceRows, int decimalPlaces = 2)
    {
        var roots = performanceRows
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId == null)
            .ToList();

        var weightedRoots = roots
            .Where(r => r.ZScore.HasValue && r.SubtreeHours is > 0)
            .ToList();

        if (weightedRoots.Count == 0)
            return null;

        var denominator = weightedRoots.Sum(r => r.SubtreeHours!.Value);
        if (denominator <= 0)
            return null;

        return Math.Round(
            weightedRoots.Sum(r => r.ZScore!.Value * r.SubtreeHours!.Value) / denominator,
            decimalPlaces);
    }
}
