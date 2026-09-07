using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>
/// Точки факт/норма: только IIC первого уровня, X = объём поддерева, Y = производительность.
/// </summary>
public static class FactNormPointSelector
{
    public sealed record SelectedPoint(
        PerformanceRowDto Row,
        decimal Volume,
        decimal Productivity);

    public static IReadOnlyList<SelectedPoint> Select(
        IReadOnlyList<PerformanceRowDto> performance,
        IReadOnlyCollection<int>? requestedWorkDirectionIds)
    {
        var parentById = performance
            .Where(r => r.WorkDirectionId > 0)
            .GroupBy(r => r.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First().ParentId);

        HashSet<int>? requestedL1 = null;
        if (requestedWorkDirectionIds is { Count: > 0 })
        {
            requestedL1 = [];
            foreach (var id in requestedWorkDirectionIds)
                requestedL1.Add(IicHierarchy.ToFirstLevelId(id, parentById));
        }

        var result = new List<SelectedPoint>();
        foreach (var row in PerformanceDisplayVolume.FirstLevel(performance))
        {
            if (requestedL1 is not null && !requestedL1.Contains(row.WorkDirectionId))
                continue;
            if (row.FactProductivity is not { } productivity)
                continue;
            var volume = PerformanceDisplayVolume.ResolveSubtree(row);
            if (volume is not > 0)
                continue;
            result.Add(new SelectedPoint(row, volume.Value, productivity));
        }

        return result;
    }

    /// <summary>
    /// Норма снимка для L1: своя запись, иначе первая норма потомка, свёрнутого в этот корень.
    /// </summary>
    public static int? ResolveSnapshotNormId(
        int firstLevelId,
        IReadOnlyDictionary<int, int> snapshotByDirection,
        IReadOnlyDictionary<int, int?> parentById)
    {
        if (snapshotByDirection.TryGetValue(firstLevelId, out var direct))
            return direct;

        foreach (var (workDirectionId, normId) in snapshotByDirection)
        {
            if (IicHierarchy.ToFirstLevelId(workDirectionId, parentById) == firstLevelId)
                return normId;
        }

        return null;
    }
}
