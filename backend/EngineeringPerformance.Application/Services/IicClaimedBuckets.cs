namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Исключающие корзины ИИС: заявленный узел собирает себя и потомков,
/// кроме поддеревьев, которые сами заявлены в регистре трудоёмкости.
/// </summary>
public static class IicClaimedBuckets
{
    public static int? NearestClaimed(
        int classifierId,
        IReadOnlySet<int> claimed,
        IReadOnlyDictionary<int, int?> parentById)
    {
        var current = classifierId;
        var guard = 0;
        while (true)
        {
            if (claimed.Contains(current))
                return current;

            if (!parentById.TryGetValue(current, out var parentId) || parentId is not int parent)
                return null;

            if (++guard > 64)
                return null;

            current = parent;
        }
    }

    /// <summary>
    /// Переносит объём классификатора на ближайший заявленный предок.
    /// Незаявленные узлы после переноса не держат свой объём.
    /// </summary>
    public static Dictionary<int, decimal> ApplyExclusiveVolumes(
        IReadOnlyDictionary<int, decimal> volumeByDirection,
        IReadOnlySet<int> claimed,
        IReadOnlyDictionary<int, int?> parentById)
    {
        var exclusive = new Dictionary<int, decimal>();
        foreach (var (id, volume) in volumeByDirection)
        {
            if (volume <= 0)
                continue;

            var bucket = NearestClaimed(id, claimed, parentById);
            if (bucket is not int target)
                continue;

            exclusive[target] = exclusive.GetValueOrDefault(target) + volume;
        }

        return exclusive;
    }
}
