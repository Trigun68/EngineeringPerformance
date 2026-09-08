using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Services;

public sealed partial class PerformanceCalculationEngine
{
    public HoursBridgeResultDto ComputeHoursBridge(
        IReadOnlyList<PerformanceRowDto> planRows,
        IReadOnlyList<PerformanceRowDto> factRows)
    {
        var planById = IndexEligible(planRows);
        var factById = IndexEligible(factRows);
        var parentById = new Dictionary<int, int?>();
        var nameById = new Dictionary<int, string>();
        CollectMeta(planRows, parentById, nameById);
        CollectMeta(factRows, parentById, nameById);

        var acc = new Dictionary<int, (decimal Volume, decimal Productivity)>();
        foreach (var (id, plan) in planById)
        {
            if (plan.Volume <= 0m || !factById.TryGetValue(id, out var fact) || fact.Volume <= 0m)
                continue;

            var hoursAtPlanRate = plan.Hours * (fact.Volume / plan.Volume);
            var volumeEffect = hoursAtPlanRate - plan.Hours;
            var productivityEffect = fact.Hours - hoursAtPlanRate;
            var rootId = FindRoot(id, parentById);
            if (!acc.TryGetValue(rootId, out var cur))
                cur = (0m, 0m);
            acc[rootId] = (cur.Volume + volumeEffect, cur.Productivity + productivityEffect);
        }

        if (acc.Count == 0)
            return HoursBridgeResultDto.Empty;

        var lines = acc
            .OrderBy(kv => kv.Key)
            .Select(kv => new HoursBridgeLineDto(
                kv.Key,
                nameById.GetValueOrDefault(kv.Key, $"Группа {kv.Key}"),
                Math.Round(kv.Value.Volume, 2, MidpointRounding.ToEven),
                Math.Round(kv.Value.Productivity, 2, MidpointRounding.ToEven)))
            .ToList();

        return new HoursBridgeResultDto(
            lines,
            lines.Sum(l => l.VolumeEffect),
            lines.Sum(l => l.ProductivityEffect));
    }

    public HoursBridgeResultDto CombineHoursBridges(IEnumerable<HoursBridgeResultDto> parts)
    {
        var acc = new Dictionary<int, (string Name, decimal Volume, decimal Productivity)>();
        foreach (var part in parts)
        {
            foreach (var line in part.Lines)
            {
                if (!acc.TryGetValue(line.GroupId, out var cur))
                    cur = (line.GroupName, 0m, 0m);
                acc[line.GroupId] = (
                    string.IsNullOrWhiteSpace(cur.Name) ? line.GroupName : cur.Name,
                    cur.Volume + line.VolumeEffect,
                    cur.Productivity + line.ProductivityEffect);
            }
        }

        if (acc.Count == 0)
            return HoursBridgeResultDto.Empty;

        var lines = acc
            .OrderBy(kv => kv.Key)
            .Select(kv => new HoursBridgeLineDto(
                kv.Key,
                kv.Value.Name,
                Math.Round(kv.Value.Volume, 2, MidpointRounding.ToEven),
                Math.Round(kv.Value.Productivity, 2, MidpointRounding.ToEven)))
            .ToList();
        return new HoursBridgeResultDto(lines, lines.Sum(l => l.VolumeEffect), lines.Sum(l => l.ProductivityEffect));
    }

    public IReadOnlyList<CumulativeHoursPointDto> ComputeCumulativeHours(IEnumerable<HoursAtDateSample> points)
    {
        decimal running = 0m;
        return points
            .OrderBy(p => p.Date)
            .Select(p =>
            {
                running += p.Hours > 0m ? p.Hours : 0m;
                return new CumulativeHoursPointDto(p.Date, Math.Round(running, 2, MidpointRounding.ToEven), p.Label);
            })
            .ToList();
    }

    public IReadOnlyList<ZeroBaselineBarDto> BuildZeroBaselineBars(
        IReadOnlyList<ZeroBaselineSample> samples,
        int topCount = 12)
    {
        var valid = samples
            .Where(s => s.ZScore is not null)
            .Select(s => (s.Name, Z: s.ZScore!.Value, s.Weight))
            .OrderByDescending(s => Math.Abs(s.Z))
            .ToList();
        if (valid.Count == 0)
            return [];

        var take = Math.Max(1, topCount);
        var top = valid.Take(take).Select(s => new ZeroBaselineBarDto(s.Name, s.Z, s.Weight)).ToList();
        var rest = valid.Skip(take).ToList();
        if (rest.Count == 0)
            return top;

        var otherZ = ComputeWeightedZ(rest.Select(s => ((decimal?)s.Z, (decimal?)s.Weight)), decimalPlaces: 2)
            ?? rest.Average(s => s.Z);
        top.Add(new ZeroBaselineBarDto("Прочие", Math.Round(otherZ, 2, MidpointRounding.ToEven), rest.Sum(s => s.Weight)));
        return top;
    }

    public HoursCoverageDto ComputeHoursCoverage(
        IReadOnlyList<PerformanceRowDto> planRows,
        IReadOnlyList<PerformanceRowDto> factRows)
    {
        var hoursTotal = SumRootHours(factRows);
        var hoursWithZ = factRows
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId is null && r.ZScore is not null)
            .Sum(RootHours);

        var planById = IndexEligible(planRows);
        var factById = IndexEligible(factRows);
        var hoursEligible = factById.Values.Sum(v => v.Hours);
        decimal hoursWithPair = 0m;
        foreach (var (id, fact) in factById)
        {
            if (!planById.TryGetValue(id, out var plan) || plan.Volume <= 0m)
                continue;
            hoursWithPair += fact.Hours;
        }

        return new HoursCoverageDto(
            Math.Round(hoursTotal, 2, MidpointRounding.ToEven),
            Math.Round(hoursWithZ, 2, MidpointRounding.ToEven),
            Math.Round(hoursWithPair, 2, MidpointRounding.ToEven),
            Math.Round(hoursEligible, 2, MidpointRounding.ToEven),
            Share(hoursWithZ, hoursTotal),
            Share(hoursWithPair, hoursEligible));
    }

    public HoursCoverageDto CombineHoursCoverages(IEnumerable<HoursCoverageDto> parts)
    {
        decimal total = 0m, withZ = 0m, withPair = 0m, eligible = 0m;
        foreach (var part in parts)
        {
            total += part.HoursTotal;
            withZ += part.HoursWithZ;
            withPair += part.HoursWithBridgePair;
            eligible += part.HoursEligibleForBridge;
        }

        return new HoursCoverageDto(
            Math.Round(total, 2, MidpointRounding.ToEven),
            Math.Round(withZ, 2, MidpointRounding.ToEven),
            Math.Round(withPair, 2, MidpointRounding.ToEven),
            Math.Round(eligible, 2, MidpointRounding.ToEven),
            Share(withZ, total),
            Share(withPair, eligible));
    }

    public decimal ComputeRawHoursDelta(
        IReadOnlyList<PerformanceRowDto> planRows,
        IReadOnlyList<PerformanceRowDto> factRows)
    {
        var factHours = SumRootHours(factRows);
        var planHours = SumRootHours(planRows);
        if (factHours == 0m && planHours == 0m)
        {
            factHours = IndexEligible(factRows).Values.Sum(v => v.Hours);
            planHours = IndexEligible(planRows).Values.Sum(v => v.Hours);
        }

        return Math.Round(factHours - planHours, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Перерасход относительно нормы IIC: факт − норматив × объём. Без округления —
    /// как в PDF-callout (PdfInsightBuilder): положительный результат = лишние часы.
    /// </summary>
    public decimal ComputeHoursOverrun(decimal factHours, decimal normProductivity, decimal volume) =>
        factHours - normProductivity * volume;

    public WeightedPercentileBandDto? ComputeWeightedPercentileBand(
        IEnumerable<(decimal ZScore, decimal Weight)> samples)
    {
        var list = samples
            .Where(s => s.Weight > 0m)
            .OrderBy(s => s.ZScore)
            .ToList();
        if (list.Count < 2)
            return null;

        var total = list.Sum(s => s.Weight);
        if (total <= 0m)
            return null;

        return new WeightedPercentileBandDto(
            WeightedPercentile(list, total, 0.10m),
            WeightedPercentile(list, total, 0.90m));
    }

    public IReadOnlyList<MoverRowDto> BuildMoverRows(IReadOnlyList<MoverSample> samples)
    {
        var valid = samples.Where(s => s.ZScore is not null).ToList();
        if (valid.Count == 0)
            return [];

        var totalWeight = valid.Sum(s => s.Weight > 0m ? s.Weight : 0m);
        var rows = valid
            .Select(s =>
            {
                decimal contribution;
                if (totalWeight > 0m)
                    contribution = s.ZScore!.Value * (s.Weight > 0m ? s.Weight : 0m) / totalWeight;
                else
                    contribution = s.ZScore!.Value / valid.Count;
                return new MoverRowDto(
                    s.ProjectId,
                    s.ProjectCode,
                    s.ProjectName,
                    s.ZScore,
                    s.Weight,
                    Math.Round(contribution, 4, MidpointRounding.ToEven));
            })
            .OrderByDescending(r => Math.Abs(r.Contribution))
            .ToList();
        return rows;
    }

    public IReadOnlyList<NamedSCurveDto> BuildTopProjectSCurves(
        IReadOnlyList<ProjectHoursSeriesSample> series,
        int topCount = 8)
    {
        var ranked = series
            .Select(s =>
            {
                var points = ComputeCumulativeHours(s.Points);
                var last = points.Count > 0 ? points[^1].CumulativeHours : 0m;
                return (s.ProjectId, s.Name, Points: points, Last: last);
            })
            .Where(x => x.Points.Count > 0)
            .OrderByDescending(x => x.Last)
            .ToList();
        if (ranked.Count == 0)
            return [];

        var take = Math.Max(1, topCount);
        var result = ranked
            .Take(take)
            .Select(x => new NamedSCurveDto(x.Name, x.Points))
            .ToList();
        var rest = ranked.Skip(take).ToList();
        if (rest.Count == 0)
            return result;

        var restIds = rest.Select(x => x.ProjectId).ToHashSet();
        var otherPoints = ComputeCumulativeHours(
            series.Where(s => restIds.Contains(s.ProjectId)).SelectMany(s => s.Points));
        if (otherPoints.Count > 0)
            result.Add(new NamedSCurveDto("Прочие", otherPoints));
        return result;
    }

    private static decimal SumRootHours(IReadOnlyList<PerformanceRowDto> rows) =>
        rows.Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId is null).Sum(RootHours);

    private static decimal RootHours(PerformanceRowDto row) =>
        row.SubtreeHours ?? row.FactLaborHours ?? 0m;

    private static decimal? Share(decimal part, decimal total) =>
        total > 0m ? Math.Round(part / total, 4, MidpointRounding.ToEven) : null;

    private static decimal WeightedPercentile(
        IReadOnlyList<(decimal ZScore, decimal Weight)> sortedAscending,
        decimal totalWeight,
        decimal p)
    {
        var target = p * totalWeight;
        decimal cumulative = 0m;
        foreach (var (z, w) in sortedAscending)
        {
            cumulative += w;
            if (cumulative >= target)
                return Math.Round(z, 2, MidpointRounding.ToEven);
        }

        return Math.Round(sortedAscending[^1].ZScore, 2, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Объём для строк с часами: свой ФО, иначе hours/P (после подъёма часов на L1 объём лежит у потомков).
    /// </summary>
    private static decimal? VolumeForHoursRow(PerformanceRowDto row)
    {
        if (row.FactPhysicalVolume is > 0m)
            return row.FactPhysicalVolume;
        if (row.FactLaborHours is > 0m && row.FactProductivity is > 0m)
            return row.FactLaborHours.Value / row.FactProductivity.Value;
        return null;
    }

    private static Dictionary<int, (decimal Volume, decimal Hours)> IndexEligible(IReadOnlyList<PerformanceRowDto> rows)
    {
        var map = new Dictionary<int, (decimal Volume, decimal Hours)>();
        foreach (var row in rows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0)
                continue;
            if (row.FactLaborHours is not decimal hours)
                continue;
            var volume = VolumeForHoursRow(row);
            if (volume is not > 0m)
                continue;
            map[row.WorkDirectionId] = (volume.Value, hours);
        }

        return map;
    }

    private static void CollectMeta(
        IReadOnlyList<PerformanceRowDto> rows,
        Dictionary<int, int?> parentById,
        Dictionary<int, string> nameById)
    {
        foreach (var row in rows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0)
                continue;
            parentById[row.WorkDirectionId] = row.ParentId;
            if (!string.IsNullOrWhiteSpace(row.WorkDirectionName))
                nameById.TryAdd(row.WorkDirectionId, row.WorkDirectionName);
        }
    }

    private static int FindRoot(int id, IReadOnlyDictionary<int, int?> parentById)
    {
        var current = id;
        for (var i = 0; i < 64; i++)
        {
            if (!parentById.TryGetValue(current, out var parent) || parent is null)
                return current;
            current = parent.Value;
        }

        return current;
    }

    public ScenarioWorkCostResult? ComputeScenarioWorkCosts(
        IEnumerable<WorkCostHourSample> hoursByDirection,
        IReadOnlyDictionary<int, decimal> ratePerHourByDirection)
    {
        ArgumentNullException.ThrowIfNull(hoursByDirection);
        ArgumentNullException.ThrowIfNull(ratePerHourByDirection);

        var hours = new Dictionary<int, (decimal Opt, decimal Base, decimal Pes)>();
        foreach (var sample in hoursByDirection)
        {
            if (sample.WorkDirectionId < 0)
                continue;

            hours.TryGetValue(sample.WorkDirectionId, out var acc);
            hours[sample.WorkDirectionId] = (
                acc.Opt + (sample.OptimisticHours ?? 0m),
                acc.Base + (sample.BaseHours ?? 0m),
                acc.Pes + (sample.PessimisticHours ?? 0m));
        }

        decimal optimistic = 0m, baseline = 0m, pessimistic = 0m;
        var matched = false;
        foreach (var (directionId, acc) in hours)
        {
            if (!ratePerHourByDirection.TryGetValue(directionId, out var rate))
                continue;

            matched = true;
            optimistic += acc.Opt * rate;
            baseline += acc.Base * rate;
            pessimistic += acc.Pes * rate;
        }

        return matched ? new ScenarioWorkCostResult(optimistic, baseline, pessimistic) : null;
    }
}
