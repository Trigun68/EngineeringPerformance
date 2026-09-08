using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Тренд Z, индекс производительности, KPI периода и факторный водопад.
/// </summary>
public sealed partial class PerformanceCalculationEngine
{
    public decimal? ComputeWeightedZFromPerformanceRows(
        IReadOnlyList<PerformanceRowDto> performanceRows,
        int decimalPlaces = 2) =>
        WeightedZScoreCalculator.FromRootPerformanceRows(performanceRows, decimalPlaces);

    public decimal? ComputeWeightedZ(
        IEnumerable<(decimal? ZScore, decimal? Weight)> samples,
        int decimalPlaces = 2,
        MidpointRounding rounding = MidpointRounding.ToEven,
        bool arithmeticFallback = false)
    {
        var aggregator = new WeightedZAggregator();
        foreach (var (zScore, weight) in samples)
            aggregator.Add(zScore, weight);

        return aggregator.Result(decimalPlaces, rounding, arithmeticFallback);
    }

    public decimal? ComputeProductivityIndex(IReadOnlyList<PerformanceRowDto> performanceRows)
    {
        decimal normHours = 0m;
        decimal hours = 0m;
        foreach (var row in performanceRows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0)
                continue;
            if (row.NormProductivity is not > 0m || row.FactLaborHours is not > 0m)
                continue;

            var volume = VolumeForHoursRow(row);
            if (volume is not > 0m)
                continue;

            normHours += volume.Value * row.NormProductivity.Value;
            hours += row.FactLaborHours.Value;
        }

        if (hours <= 0m || normHours <= 0m)
            return null;

        return Math.Round(normHours / hours * 100m, 1, MidpointRounding.ToEven);
    }

    public IReadOnlyList<MonthlyTrendBucketDto> BucketTrendPointsByMonth(IEnumerable<TrendPointSample> points)
    {
        // Σ(v×w)/Σ(w) только через WeightedZAggregator. Без Result() — ручной код не округлял
        // (тест WeightedAverage ждёт 0.4375, а не 0.44). Нулевой вес → арифметика, как раньше.
        return points
            .Where(p => p.Value is not null)
            .GroupBy(p => (p.Date.Year, p.Date.Month))
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var aggregator = new WeightedZAggregator();
                foreach (var point in g)
                    aggregator.Add(point.Value, point.Weight);

                var date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                return new MonthlyTrendBucketDto(date, aggregator.Mean(arithmeticFallback: true));
            })
            .ToList();
    }

    public PeriodKpiDto ComputePeriodKpi(IReadOnlyList<MonthlyTrendBucketDto> buckets)
    {
        var valid = buckets.Where(b => b.Value is not null).ToList();
        if (valid.Count == 0)
            return new PeriodKpiDto(null, null, null);

        var initial = valid[0].Value;
        var current = valid[^1].Value;
        decimal? delta = current is not null && initial is not null ? current - initial : null;
        return new PeriodKpiDto(current, initial, delta);
    }

    public IReadOnlyList<WaterfallStepDto>? BuildFactorWaterfall(
        IReadOnlyList<TrendGroupSample> groups,
        decimal? overallDelta,
        int topCount = 3)
    {
        if (overallDelta is null || groups.Count == 0)
            return null;

        var totalWeight = groups.Sum(g => g.Points.Sum(p => p.Weight > 0m ? p.Weight : 0m));
        if (totalWeight <= 0m)
            return null;

        var contributions = new List<(string Name, decimal Value)>();
        foreach (var group in groups)
        {
            var kpi = ComputePeriodKpi(BucketTrendPointsByMonth(group.Points));
            if (kpi.Delta is null)
                continue;

            var weight = group.Points.Sum(p => p.Weight > 0m ? p.Weight : 0m);
            contributions.Add((group.GroupName, kpi.Delta.Value * (weight / totalWeight)));
        }

        if (contributions.Count == 0)
            return null;

        var ordered = contributions.OrderByDescending(c => Math.Abs(c.Value)).ToList();
        var top = ordered.Take(Math.Max(1, topCount)).ToList();
        var topSum = top.Sum(c => c.Value);
        var rest = overallDelta.Value - topSum;

        var steps = top.Select(c => new WaterfallStepDto(c.Name, c.Value)).ToList();
        if (ordered.Count > topCount || Math.Abs(rest) > 1e-9m)
            steps.Add(new WaterfallStepDto("Прочие направления", rest));

        return steps;
    }

    public TrendDetailAggregationDto BuildTrendDetailAggregation(
        IReadOnlyList<TrendPointSample> planPoints,
        IReadOnlyList<TrendPointSample> factPoints,
        IReadOnlyList<TrendGroupSample> factGroups,
        int waterfallTopCount = 3,
        IReadOnlyList<TrendGroupSample>? planGroups = null)
    {
        var planMonthly = BucketTrendPointsByMonth(planPoints);
        var factMonthly = BucketTrendPointsByMonth(factPoints);
        var planKpi = ComputePeriodKpi(planMonthly);
        var factKpi = ComputePeriodKpi(factMonthly);
        var factWaterfall = BuildFactorWaterfall(factGroups, factKpi.Delta, waterfallTopCount);
        var planWaterfall = planGroups is { Count: > 0 }
            ? BuildFactorWaterfall(planGroups, planKpi.Delta, waterfallTopCount)
            : null;
        return new TrendDetailAggregationDto(
            planMonthly, factMonthly, planKpi, factKpi, factWaterfall, planWaterfall);
    }
}
