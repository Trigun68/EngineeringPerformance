using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Registers;

public readonly record struct WorkCostDirectionNode(int Id, int? ParentId);

/// <summary>
/// Раскладка ставки по дереву ИИС. Правила глубины совпадают с <see cref="ZParameterLevel"/>.
/// </summary>
public static class WorkCostCascade
{
    public static IReadOnlyDictionary<int, decimal> Expand(
        IReadOnlyList<WorkCostDirectionNode> directions,
        WorkCostLevel level,
        IReadOnlyList<WorkCostBatchItem>? items)
    {
        if (!Enum.IsDefined(level))
            throw new InvalidOperationException("Некорректный уровень задания стоимости.");

        var list = items ?? Array.Empty<WorkCostBatchItem>();
        if (directions.Count == 0)
            return new Dictionary<int, decimal>();

        var byId = new Dictionary<int, WorkCostDirectionNode>(directions.Count);
        foreach (var node in directions)
            byId[node.Id] = node;

        foreach (var item in list)
        {
            if (item.RatePerHour < 0)
                throw new InvalidOperationException("Стоимость не может быть отрицательной.");

            if (item.WorkDirectionId == 0)
                continue;

            if (!byId.ContainsKey(item.WorkDirectionId))
                throw new InvalidOperationException($"Направление работ {item.WorkDirectionId} не найдено.");
        }

        if (level == WorkCostLevel.Organization)
        {
            var org = list.FirstOrDefault(i => i.WorkDirectionId == 0) ?? list.FirstOrDefault();
            if (org is null)
                throw new InvalidOperationException("Задайте стоимость в целом по организации.");

            return directions.ToDictionary(d => d.Id, _ => org.RatePerHour);
        }

        var applicable = list.Where(i => i.WorkDirectionId != 0).ToList();
        if (applicable.Count == 0)
            throw new InvalidOperationException("Не задана ни одна ставка.");

        if (level == WorkCostLevel.EachElement)
            return applicable.ToDictionary(i => i.WorkDirectionId, i => i.RatePerHour);

        var childrenByParent = new Dictionary<int, List<int>>();
        foreach (var node in directions)
        {
            if (node.ParentId is not int parentId)
                continue;
            if (!childrenByParent.TryGetValue(parentId, out var children))
            {
                children = [];
                childrenByParent[parentId] = children;
            }

            children.Add(node.Id);
        }

        var depth = BuildDepthMap(directions);
        var result = new Dictionary<int, decimal>();
        foreach (var item in applicable.OrderBy(i => depth[i.WorkDirectionId]))
        {
            foreach (var id in CollectSubtree(item.WorkDirectionId, childrenByParent))
                result[id] = item.RatePerHour;
        }

        return result;
    }

    public static Dictionary<int, int> BuildDepthMap(IReadOnlyList<WorkCostDirectionNode> directions)
    {
        var byId = directions.ToDictionary(d => d.Id);
        var cache = new Dictionary<int, int>();

        int DepthOf(int id)
        {
            if (cache.TryGetValue(id, out var cached))
                return cached;
            if (!byId.TryGetValue(id, out var node) || node.ParentId is null)
            {
                cache[id] = 0;
                return 0;
            }

            var depth = DepthOf(node.ParentId.Value) + 1;
            cache[id] = depth;
            return depth;
        }

        foreach (var node in directions)
            DepthOf(node.Id);

        return cache;
    }

    private static List<int> CollectSubtree(int rootId, IReadOnlyDictionary<int, List<int>> childrenByParent)
    {
        var result = new List<int>();
        void Walk(int id)
        {
            result.Add(id);
            if (!childrenByParent.TryGetValue(id, out var children))
                return;
            foreach (var childId in children)
                Walk(childId);
        }

        Walk(rootId);
        return result;
    }
}
