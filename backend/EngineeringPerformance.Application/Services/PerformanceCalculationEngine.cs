using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Вся математика агрегации ИИС, Z-score и распределения LI — без доступа к БД.
/// </summary>
public sealed class PerformanceCalculationEngine : IPerformanceCalculationEngine
{
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
                    factProductivity = Math.Round(setHours / setVol, 4);
                    if (factProductivity > 0
                        && norm?.NormEstimate is decimal ne
                        && norm.NormStdDev is decimal nsd
                        && nsd > 0)
                    {
                        var factEstimate = (decimal)Math.Log((double)factProductivity.Value);
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
                zScore));
        }

        foreach (var eq in input.Equipment
                     .OrderBy(e => e.FacilityCode)
                     .ThenBy(e => e.FacilityName)
                     .ThenBy(e => e.PvName))
        {
            input.NormByDirection.TryGetValue(eq.ClassifierId ?? -1, out var norm);
            var vol = eq.Quantity * eq.Coefficient;
            decimal? factHours = null;
            if (eq.IsAssessment && norm?.NormProductivity is decimal p && vol != 0)
                factHours = Math.Round(vol * p, 2);

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
                factHours,
                null,
                null));
        }

        var facilityRows = detailRows
            .Where(r => !string.IsNullOrEmpty(r.FacilityCode))
            .GroupBy(r => r.FacilityCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                var hoursRows = g.Where(r => r.FactLaborHours.HasValue && r.ZScore.HasValue).ToList();
                decimal? totalHours = g.Any(r => r.FactLaborHours.HasValue)
                    ? Math.Round(g.Sum(r => r.FactLaborHours ?? 0m), 2)
                    : null;

                decimal? facilityZ = null;
                if (hoursRows.Count > 0)
                {
                    var sumHours = hoursRows.Sum(r => r.FactLaborHours!.Value);
                    if (sumHours > 0)
                    {
                        var weighted = hoursRows.Sum(r => r.ZScore!.Value * r.FactLaborHours!.Value);
                        facilityZ = Math.Round(weighted / sumHours, 4);
                    }
                }

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
                null));
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
        return string.IsNullOrWhiteSpace(node.UnitName)
            ? $"{node.Code} {node.Name}"
            : $"{node.Code} {node.Name} ({node.UnitName})";
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

        decimal? factProductivity = factVolume > 0 ? factHours / factVolume : null;

        decimal? ownFactEstimate = null;
        if (factProductivity is > 0)
            ownFactEstimate = (decimal)Math.Log((double)factProductivity.Value);

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

    public (decimal Optimistic, decimal Pessimistic, string Range) ComputeForecastScenarioHours(
        decimal baseHours,
        decimal? performance,
        decimal? standardDeviation,
        decimal plannedVolume,
        decimal zOptimistic,
        decimal zPessimistic)
    {
        if (baseHours <= 0 || plannedVolume <= 0)
        {
            return (0m, 0m, "—");
        }

        if (performance is { } p && p > 0 && standardDeviation is { } std && std > 0)
        {
            var opt = Math.Round(plannedVolume * Math.Max(0m, p + zOptimistic * std), 2);
            var pes = Math.Round(plannedVolume * (p + zPessimistic * std), 2);
            return (opt, pes, $"{opt:F2} - {pes:F2}");
        }

        return (baseHours, baseHours, $"{baseHours:F2} - {baseHours:F2}");
    }
}
