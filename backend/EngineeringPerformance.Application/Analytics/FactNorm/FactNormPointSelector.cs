using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>
/// Точки факт/норма: узлы с собственными факт-часами (L1 после свёртки или заявленный узел).
/// X = объём узла, Y = производительность.
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

        HashSet<int>? requested = null;
        if (requestedWorkDirectionIds is { Count: > 0 })
            requested = requestedWorkDirectionIds.ToHashSet();

        var hourBearingIds = performance
            .Where(r => !r.IsTotal && r.WorkDirectionId > 0 && r.FactLaborHours is > 0)
            .Select(r => r.WorkDirectionId)
            .ToHashSet();

        var result = new List<SelectedPoint>();
        foreach (var row in performance.Where(r => !r.IsTotal && r.WorkDirectionId > 0 && r.FactLaborHours is > 0))
        {
            if (requested is not null
                && !requested.Contains(row.WorkDirectionId)
                && !CoversRolledRequest(row.WorkDirectionId, requested, hourBearingIds, parentById))
            {
                continue;
            }
            if (row.FactProductivity is not { } productivity)
                continue;
            var volume = row.FactPhysicalVolume is > 0
                ? row.FactPhysicalVolume.Value
                : PerformanceDisplayVolume.ResolveSubtree(row);
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

    /// <summary>
    /// Запрос листа без собственных часов (свёртка на L1) попадает на корень, где часы уже лежат.
    /// Заявленный узел с часами не схлопывается в предка.
    /// </summary>
    private static bool CoversRolledRequest(
        int rowId,
        IReadOnlySet<int> requested,
        IReadOnlySet<int> hourBearingIds,
        IReadOnlyDictionary<int, int?> parentById)
    {
        foreach (var id in requested)
        {
            if (hourBearingIds.Contains(id))
                continue;
            if (IicHierarchy.ToFirstLevelId(id, parentById) == rowId)
                return true;
        }

        return false;
    }
}
