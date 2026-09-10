using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Сопоставление Excel/JSON «тип документа TF» со справочником «Трудоёмкость»:
/// <see cref="LaborIntensity.DocumentTypeName"/> или <see cref="LaborIntensity.Name"/>.
/// </summary>
internal static class LaborCatalogLookup
{
    public static Dictionary<string, LaborIntensity> BuildMap(IEnumerable<LaborIntensity> rows)
    {
        var map = new Dictionary<string, LaborIntensity>(StringComparer.OrdinalIgnoreCase);
        var list = rows as IList<LaborIntensity> ?? rows.ToList();
        foreach (var li in list)
            IndexKey(map, li.DocumentTypeName, li);
        foreach (var li in list)
            IndexKey(map, li.Name, li);
        return map;
    }

    public static HashSet<string> KnownKeys(IEnumerable<LaborIntensity> rows)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in rows)
        {
            Add(set, li.DocumentTypeName);
            Add(set, li.Name);
        }

        return set;
    }

    public static Dictionary<string, int> FolderIdsByName(IEnumerable<LaborIntensity> rows)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var li in rows)
        {
            if (li.IicWorkDirectionId.HasValue)
                continue;
            var name = (li.Name ?? string.Empty).Trim();
            if (name.Length == 0 || map.ContainsKey(name))
                continue;
            map[name] = li.Id;
        }

        return map;
    }

    private static void IndexKey(Dictionary<string, LaborIntensity> map, string? raw, LaborIntensity item)
    {
        var key = (raw ?? string.Empty).Trim();
        if (key.Length == 0)
            return;

        if (!map.TryGetValue(key, out var existing) || (existing.IsArchived && !item.IsArchived))
            map[key] = item;
    }

    private static void Add(HashSet<string> set, string? raw)
    {
        var key = (raw ?? string.Empty).Trim();
        if (key.Length > 0)
            set.Add(key);
    }
}
