using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Единый расчёт средневзвешенного Z по корневым направлениям.
/// Internal-реализация <c>IPerformanceCalculationEngine.ComputeWeightedZFromPerformanceRows</c>;
/// Infrastructure и тесты обращаются только через движок.
/// </summary>
internal static class WeightedZScoreCalculator
{
    public static decimal? FromRootPerformanceRows(IReadOnlyList<PerformanceRowDto> performanceRows, int decimalPlaces = 2)
    {
        var aggregator = new WeightedZAggregator();
        foreach (var row in performanceRows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0 || row.ParentId is not null)
                continue;

            aggregator.Add(row.ZScore, row.SubtreeHours);
        }

        return aggregator.Result(decimalPlaces);
    }
}
