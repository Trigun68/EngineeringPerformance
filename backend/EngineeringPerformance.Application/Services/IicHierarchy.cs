namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Подъём по дереву IIC до узла первого уровня (ParentId == null).
/// </summary>
internal static class IicHierarchy
{
    public static int ToFirstLevelId(int id, IReadOnlyDictionary<int, int?> parentById)
    {
        var current = id;
        var guard = 0;
        while (parentById.TryGetValue(current, out var parentId) && parentId is int parent)
        {
            if (++guard > 64)
                break;
            current = parent;
        }

        return current;
    }
}
