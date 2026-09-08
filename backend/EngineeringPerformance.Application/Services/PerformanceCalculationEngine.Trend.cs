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
        var buckets = new Dictionary<(int Year, int Month), (decimal WeightedSum, decimal WeightSum, decimal ValueSum, int Count)>();
        foreach (var point in points)
        {
            if (point.Value is null)
                continue;

            var key = (point.Date.Year, point.Date.Month);
            var weight = point.Weight > 0m ? point.Weight : 0m;
            if (!buckets.TryGetValue(key, out var bucket))
                bucket = (0m, 0m, 0m, 0);

            buckets[key] = (
                bucket.WeightedSum + point.Value.Value * weight,
                bucket.WeightSum + weight,
                bucket.ValueSum + point.Value.Value,
                bucket.Count + 1);
        }

        return buckets
            .OrderBy(kv => kv.Key.Year)
            .ThenBy(kv => kv.Key.Month)
            .Select(kv =>
            {
                var date = new DateTime(kv.Key.Year, kv.Key.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
                decimal? value = kv.Value.WeightSum > 0m
                    ? kv.Value.WeightedSum / kv.Value.WeightSum
                    : kv.Value.ValueSum / kv.Value.Count;
                return new MonthlyTrendBucketDto(date, value);
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
