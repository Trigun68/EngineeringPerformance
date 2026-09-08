using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Иерархия производительности ИИС: объём, часы, Z-score по дереву направлений.
/// </summary>
public sealed partial class PerformanceCalculationEngine
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

        if (input.RollHoursToFirstLevel)
            hoursByDirection = RollHoursToFirstLevel(hoursByDirection, input.WorkDirections);

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

    private static Dictionary<int, decimal> RollHoursToFirstLevel(
        Dictionary<int, decimal> hoursByDirection,
        IReadOnlyList<WorkDirectionCalculationNode> directions)
    {
        var parentById = directions.ToDictionary(d => d.Id, d => d.ParentId);
        var rolled = new Dictionary<int, decimal>();
        foreach (var (id, hours) in hoursByDirection)
        {
            var target = parentById.ContainsKey(id)
                ? IicHierarchy.ToFirstLevelId(id, parentById)
                : id;
            rolled[target] = rolled.GetValueOrDefault(target) + hours;
        }

        return rolled;
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

    private static (decimal SubtreeEstimateHours, decimal SubtreeWeightedEstimate, decimal SubtreeVolume) BuildHierarchy(
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
            return (0m, 0m, 0m);

        var ownVolume = volumeByDirection.GetValueOrDefault(direction.Id);
        var factHours = hoursByDirection.GetValueOrDefault(direction.Id);

        var rowIndex = result.Count;
        result.Add(null!);

        decimal subtreeVolume = ownVolume;
        decimal subtreeEstimateHours = 0m;
        decimal subtreeWeightedEstimate = 0m;

        foreach (var child in parentIdLookup[direction.Id])
        {
            var (childEstimateHours, childWeighted, childVolume) = BuildHierarchy(
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
            subtreeVolume += childVolume;
        }

        var volumeForProductivity = factHours > 0 ? subtreeVolume : ownVolume;
        decimal? factProductivity = volumeForProductivity > 0 && factHours > 0
            ? Math.Round(factHours / volumeForProductivity, 4)
            : null;

        decimal? ownFactEstimate = null;
        if (factProductivity is > 0)
            ownFactEstimate = ProductivityToEstimate(factProductivity.Value);

        if (ownFactEstimate.HasValue && factHours > 0)
        {
            subtreeEstimateHours += factHours;
            subtreeWeightedEstimate += ownFactEstimate.Value * factHours;
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
            FactPhysicalVolume: ownVolume > 0 ? ownVolume : null,
            FactLaborHours: factHours > 0 ? factHours : null,
            FactProductivity: factProductivity,
            FactEstimate: aggregatedFactEstimate,
            NormProductivity: norm?.Performance,
            NormEstimate: norm?.Estimate,
            NormStdDev: norm?.StandardDeviation,
            ZScore: zScore,
            Interpretation: interpretation,
            SubtreeHours: subtreeEstimateHours > 0 ? subtreeEstimateHours : null,
            SubtreePhysicalVolume: subtreeVolume > 0 ? subtreeVolume : null);

        return (subtreeEstimateHours, subtreeWeightedEstimate, subtreeVolume);
    }
}
