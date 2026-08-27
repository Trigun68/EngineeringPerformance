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
        LaborIntensityCalculator.Recalculate(
            scenario, lines, normsByDirection, planningZByDirection, _deviationCalculator);

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
        IReadOnlyList<LaborIntensityCalculator.EquipmentLine> lines,
        IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
        IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection) =>
        ForecastHoursAggregator.ComputeEquipmentHours(
            lines, normsByDirection, planningZByDirection, _deviationCalculator);

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
}
