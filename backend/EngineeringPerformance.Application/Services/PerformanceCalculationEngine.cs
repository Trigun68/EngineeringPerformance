using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Вся математика агрегации ИИС, Z-score и распределения LI — без доступа к БД.
/// </summary>
public sealed class PerformanceCalculationEngine : IPerformanceCalculationEngine
{
    private readonly IDeviationCalculator _deviationCalculator;

    /// <summary>
    /// Оценка нормы/факта в лог-шкале: Estimate = ln(производительность), при 0 → 0.
    /// Конвенция хранения норм ИИС — единственный SoT перехода производительность → estimate.
    /// </summary>
    public static decimal ProductivityToEstimate(decimal productivity) =>
        productivity > 0 ? (decimal)Math.Log((double)productivity) : 0m;

    public PerformanceCalculationEngine(IDeviationCalculator deviationCalculator)
    {
        _deviationCalculator = deviationCalculator;
    }
    public IReadOnlyList<PerformanceRowDto> CalculatePerformance(PerformanceCalculationInput input)
    {
        if (input.WorkDirections.Count == 0 && !input.VersionExists)
        {
            return
            [
                CreateUnallocatedPlaceholderRow(0m)
            ];
        }

        var parentIdLookup = input.WorkDirections
            .Where(d => d.ParentId.HasValue)
            .ToLookup(d => d.ParentId!.Value);

        var volumeByDirection = input.PhysicalVolumes
            .Where(pv => pv.ClassifierId.HasValue && pv.IsAssessment)
            .GroupBy(pv => pv.ClassifierId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.Quantity * pv.Coefficient));

        MergeEquipmentVolumes(volumeByDirection, input.Equipment);

        var hoursByDirection = input.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue)
            .GroupBy(li => li.WorkDirectionId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(li => li.PlannedHours));

        var rootDirections = input.WorkDirections.Where(d => !d.ParentId.HasValue).ToList();
        var result = new List<PerformanceRowDto>();
        var visited = new HashSet<int>();

        foreach (var root in rootDirections)
        {
            BuildHierarchy(
                root,
                0,
                parentIdLookup,
                volumeByDirection,
                hoursByDirection,
                input.NormsByDirection,
                result,
                visited);
        }

        var unallocatedPhysicalVolume = input.PhysicalVolumes
            .Where(pv => !pv.ClassifierId.HasValue && pv.IsAssessment)
            .Sum(pv => pv.Quantity * pv.Coefficient)
            + SumUnallocatedEquipmentVolume(input.Equipment);

        result.Add(CreateUnallocatedPlaceholderRow(unallocatedPhysicalVolume));
        return result;
    }

    public FactAssessmentCalculationResult CalculateFactAssessment(FactAssessmentCalculationInput input)
    {
        var hoursByDocSetDir = input.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue && !string.IsNullOrEmpty(li.DocSet))
            .GroupBy(li => (Set: li.DocSet, Dir: li.WorkDirectionId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Hours));

        var docSetVolumes = input.PhysicalVolumes
            .Where(pv => pv.IsAssessment && pv.ClassifierId.HasValue && !string.IsNullOrEmpty(pv.DocumentSet))
            .GroupBy(pv => (Set: pv.DocumentSet, Dir: pv.ClassifierId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.Quantity * pv.Coefficient));

        var detailRows = new List<FactAssessmentDetailRowDto>();
        decimal totalMatchedHours = 0m;

        foreach (var pv in input.PhysicalVolumes
                     .OrderBy(pv => pv.FacilityCode)
                     .ThenBy(pv => pv.FacilityName)
                     .ThenBy(pv => pv.PvCode))
        {
            input.NormByDirection.TryGetValue(pv.ClassifierId ?? -1, out var norm);

            var vol = pv.Quantity * pv.Coefficient;
            decimal? factHours = null;
            decimal? factProductivity = null;
            decimal? zScore = null;

            if (pv.IsAssessment && pv.ClassifierId.HasValue && vol > 0 && !string.IsNullOrEmpty(pv.DocumentSet))
            {
                var key = (Set: pv.DocumentSet, Dir: pv.ClassifierId.Value);
                if (docSetVolumes.TryGetValue(key, out var setVol)
                    && setVol > 0
                    && hoursByDocSetDir.TryGetValue(key, out var setHours)
                    && setHours > 0)
                {
                    factHours = Math.Round(setHours * (vol / setVol), 2);
                    totalMatchedHours += factHours.Value;
                    factProductivity = Math.Max(0m, Math.Round(setHours / setVol, 4));
                    if (factProductivity > 0
                        && norm?.NormEstimate is decimal ne
                        && norm.NormStdDev is decimal nsd
                        && nsd > 0)
                    {
                        var factEstimate = ProductivityToEstimate(factProductivity.Value);
                        zScore = Math.Round((factEstimate - ne) / nsd, 4);
                    }
                }
            }

            detailRows.Add(new FactAssessmentDetailRowDto(
                pv.FacilityCode,
                pv.FacilityName,
                pv.DocumentSet,
                pv.PvCode,
                pv.PvName,
                pv.WorkId,
                pv.WorkName,
                FormatClassifierName(pv.ClassifierId, input.ClassifierMap),
                norm?.NormUnitName ?? string.Empty,
                pv.Quantity,
                factHours,
                factProductivity,
                zScore,
                pv.SourceRowId,
                IsEquipment: false));
        }

        // Оборудование: только объём в гриде. Факт. трудоёмкость — исключительно из LI
        // (доля DocSet×направление на PPV). Не считать vol×NormProductivity — это формула прогноза.
        foreach (var eq in input.Equipment
                     .OrderBy(e => e.FacilityCode)
                     .ThenBy(e => e.FacilityName)
                     .ThenBy(e => e.PvName))
        {
            input.NormByDirection.TryGetValue(eq.ClassifierId ?? -1, out var norm);

            detailRows.Add(new FactAssessmentDetailRowDto(
                eq.FacilityCode,
                eq.FacilityName,
                eq.DocumentSet,
                eq.PvCode,
                eq.PvName,
                string.Empty,
                string.Empty,
                FormatClassifierName(eq.ClassifierId, input.ClassifierMap),
                norm?.NormUnitName ?? string.Empty,
                eq.Quantity,
                FactLaborHours: null,
                FactProductivity: null,
                ZScore: null,
                eq.SourceRowId,
                IsEquipment: true));
        }

        var facilityRows = detailRows
            .Where(r => !string.IsNullOrEmpty(r.FacilityCode))
            .GroupBy(r => r.FacilityCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                decimal? totalHours = g.Any(r => r.FactLaborHours.HasValue)
                    ? Math.Round(g.Sum(r => r.FactLaborHours ?? 0m), 2)
                    : null;

                var facilityZ = ComputeWeightedZ(
                    g.Select(r => (r.ZScore, r.FactLaborHours)),
                    decimalPlaces: 4);

                var interp = PerformanceInterpretationMapper.FromZScore(facilityZ);
                return new FactAssessmentFacilityRowDto(
                    g.Key,
                    first.FacilityName,
                    totalHours,
                    facilityZ,
                    interp?.ToRussian() ?? string.Empty);
            })
            .ToList();

        var totalLiHours = input.LaborIntensities.Sum(li => li.Hours);
        var unmatchedHours = Math.Round(totalLiHours - totalMatchedHours, 2);
        if (unmatchedHours > 0)
        {
            facilityRows.Add(new FactAssessmentFacilityRowDto(
                string.Empty,
                "(пусто)",
                unmatchedHours,
                null,
                string.Empty));

            detailRows.Add(new FactAssessmentDetailRowDto(
                string.Empty,
                "(пусто)",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0m,
                unmatchedHours,
                null,
                null,
                SourceRowId: null,
                IsEquipment: false));
        }

        return new FactAssessmentCalculationResult(detailRows, facilityRows);
    }

    private static PerformanceRowDto CreateUnallocatedPlaceholderRow(decimal unallocatedVolume) =>
        new(
            WorkDirectionId: PerformanceRowDto.UnallocatedPhysicalVolumeWorkDirectionId,
            WorkDirectionName: "(пусто)",
            WorkDirectionCode: string.Empty,
            ParentId: null,
            Level: -1,
            NormUnitName: null,
            FactPhysicalVolume: unallocatedVolume > 0 ? unallocatedVolume : null,
            FactLaborHours: null,
            FactProductivity: null,
            FactEstimate: null,
            NormProductivity: null,
            NormEstimate: null,
            NormStdDev: null,
            ZScore: null,
            Interpretation: null,
            SubtreeHours: null);

    private static void MergeEquipmentVolumes(
        Dictionary<int, decimal> volumeByDirection,
        IReadOnlyList<EquipmentCalculationLine> equipment)
    {
        foreach (var group in equipment
                     .Where(e => e.ClassifierId.HasValue && e.IsAssessment)
                     .GroupBy(e => e.ClassifierId!.Value))
        {
            var sum = group.Sum(e => e.Quantity * e.Coefficient);
            if (volumeByDirection.TryGetValue(group.Key, out var existing))
                volumeByDirection[group.Key] = existing + sum;
            else
                volumeByDirection[group.Key] = sum;
        }
    }

    private static decimal SumUnallocatedEquipmentVolume(IReadOnlyList<EquipmentCalculationLine> equipment) =>
        equipment
            .Where(e => !e.ClassifierId.HasValue && e.IsAssessment)
            .Sum(e => e.Quantity * e.Coefficient);

    private static string FormatClassifierName(
        int? classifierId,
        IReadOnlyDictionary<int, ClassifierDisplayNode> classifierMap)
    {
        if (classifierId is not int cid || !classifierMap.TryGetValue(cid, out var node))
            return string.Empty;
        return IicWorkDirectionDisplayUnit.FormatCodeNameWithUnit(node.Code, node.Name, node.UnitName);
    }

    private static (decimal SubtreeEstimateHours, decimal SubtreeWeightedEstimate) BuildHierarchy(
        WorkDirectionCalculationNode direction,
        int level,
        ILookup<int, WorkDirectionCalculationNode> parentIdLookup,
        IReadOnlyDictionary<int, decimal> volumeByDirection,
        IReadOnlyDictionary<int, decimal> hoursByDirection,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normByDirection,
        List<PerformanceRowDto> result,
        HashSet<int> visited)
    {
        if (!visited.Add(direction.Id))
            return (0m, 0m);

        var factVolume = volumeByDirection.GetValueOrDefault(direction.Id);
        var factHours = hoursByDirection.GetValueOrDefault(direction.Id);

        decimal? factProductivity = factVolume > 0 && factHours > 0
            ? Math.Round(factHours / factVolume, 4)
            : null;

        decimal? ownFactEstimate = null;
        if (factProductivity is > 0)
            ownFactEstimate = ProductivityToEstimate(factProductivity.Value);

        decimal subtreeEstimateHours = ownFactEstimate.HasValue && factHours > 0 ? factHours : 0m;
        decimal subtreeWeightedEstimate = ownFactEstimate.HasValue && factHours > 0
            ? ownFactEstimate.Value * factHours
            : 0m;

        var rowIndex = result.Count;
        result.Add(null!);

        foreach (var child in parentIdLookup[direction.Id])
        {
            var (childEstimateHours, childWeighted) = BuildHierarchy(
                child,
                level + 1,
                parentIdLookup,
                volumeByDirection,
                hoursByDirection,
                normByDirection,
                result,
                visited);
            subtreeEstimateHours += childEstimateHours;
            subtreeWeightedEstimate += childWeighted;
        }

        decimal? aggregatedFactEstimate = subtreeEstimateHours > 0
            ? subtreeWeightedEstimate / subtreeEstimateHours
            : ownFactEstimate;

        normByDirection.TryGetValue(direction.Id, out var norm);

        decimal? zScore = null;
        if (aggregatedFactEstimate.HasValue
            && norm?.Estimate is decimal normEstimate
            && norm.StandardDeviation is decimal stdDev
            && stdDev > 0)
        {
            zScore = (aggregatedFactEstimate.Value - normEstimate) / stdDev;
        }

        var interpretation = PerformanceInterpretationMapper.FromZScore(zScore);

        result[rowIndex] = new PerformanceRowDto(
            WorkDirectionId: direction.Id,
            WorkDirectionName: direction.Name,
            WorkDirectionCode: direction.Code,
            ParentId: direction.ParentId,
            Level: level,
            NormUnitName: direction.NormUnitName,
            FactPhysicalVolume: factVolume > 0 ? factVolume : null,
            FactLaborHours: factHours > 0 ? factHours : null,
            FactProductivity: factProductivity,
            FactEstimate: aggregatedFactEstimate,
            NormProductivity: norm?.Performance,
            NormEstimate: norm?.Estimate,
            NormStdDev: norm?.StandardDeviation,
            ZScore: zScore,
            Interpretation: interpretation,
            SubtreeHours: subtreeEstimateHours > 0 ? subtreeEstimateHours : null);

        return (subtreeEstimateHours, subtreeWeightedEstimate);
    }

    public decimal ComputeForecastBaseHours(
        decimal plannedVolume,
        decimal? performance,
        decimal? estimate,
        decimal laborHours,
        string method)
    {
        if (plannedVolume > 0)
        {
            if (string.Equals(method, "element", StringComparison.OrdinalIgnoreCase)
                && estimate is { } est && est > 0)
            {
                return Math.Round(plannedVolume * est, 2);
            }

            if (performance is { } p && p > 0)
            {
                return Math.Round(plannedVolume * p, 2);
            }
        }

        return laborHours;
    }

    public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        PlanningZParameters z) =>
        ForecastCalculation.ComputeForecastHours(normFormula, deviationFormula, volume, effectiveVolume, z, _deviationCalculator);

    public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
        decimal volume,
        decimal? productivity,
        decimal? stdDev,
        PlanningZParameters z) =>
        ForecastCalculation.ComputeForecastHours(volume, productivity, stdDev, z);

    public IReadOnlyList<LaborIntensityCalculator.RecalcResult> RecalculateLaborIntensities(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        RecalculatePlanHours(
            scenario, lines, Array.Empty<LaborIntensityCalculator.EquipmentLine>(),
            normsByDirection, planningZByDirection).Ppv;

    public LaborIntensityCalculator.PlanHoursResult RecalculatePlanHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> ppvLines,
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> equipmentLines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        LaborIntensityCalculator.Recalculate(
            scenario, ppvLines, equipmentLines, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        ForecastHoursAggregator.ComputePpvHours(
            scenario, lines, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
        IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
        IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
        IEnumerable<(int ClassifierId, decimal VolumeInIicUnits)> equipmentVolumes,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        ForecastHoursAggregator.AggregateByDirection(
            lines, ppvHoursById, equipmentVolumes, normsByDirection, planningZByDirection, _deviationCalculator);

    public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeEquipmentForecastHours(
        PlanningScenario scenario,
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> lines,
        IReadOnlyList<LaborIntensityCalculator.PpvLine> ppvLines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        RecalculatePlanHours(scenario, ppvLines, lines, normsByDirection, planningZByDirection).Equipment;

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

    public PlanningZParameters ResolvePlanningZ(
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection,
        int workDirectionId) =>
        ForecastCalculation.ResolvePlanningZ(planningZByDirection, workDirectionId);

    public decimal? ComputeProductivityIndex(IReadOnlyList<PerformanceRowDto> performanceRows)
    {
        decimal normHours = 0m;
        decimal hours = 0m;
        foreach (var row in performanceRows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0)
                continue;
            if (row.FactPhysicalVolume is not > 0m || row.NormProductivity is not > 0m)
                continue;
            if (row.FactLaborHours is not > 0m)
                continue;

            normHours += row.FactPhysicalVolume.Value * row.NormProductivity.Value;
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

    private static Dictionary<int, (decimal Volume, decimal Hours)> IndexEligible(IReadOnlyList<PerformanceRowDto> rows)
    {
        var map = new Dictionary<int, (decimal Volume, decimal Hours)>();
        foreach (var row in rows)
        {
            if (row.IsTotal || row.WorkDirectionId < 0)
                continue;
            if (row.FactPhysicalVolume is not > 0m || row.FactLaborHours is not decimal hours)
                continue;
            map[row.WorkDirectionId] = (row.FactPhysicalVolume.Value, hours);
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
