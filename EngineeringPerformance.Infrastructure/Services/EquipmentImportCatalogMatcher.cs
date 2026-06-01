namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>Сопоставление строк импорта оборудования со справочником по точному наименованию.</summary>
public static class EquipmentImportCatalogMatcher
{
    public static Dictionary<string, Data.Equipment> BuildNameLookup(IEnumerable<Data.Equipment> items)
    {
        var lookup = new Dictionary<string, Data.Equipment>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                continue;

            lookup.TryAdd(item.Name, item);
        }

        return lookup;
    }

    /// <summary>
    /// Находит элемент по наименованию или создаёт новый с пустой классификацией.
    /// </summary>
    public static (Data.Equipment Equipment, bool Created) ResolveOrCreate(
        string catalogName,
        Dictionary<string, Data.Equipment> lookup,
        IList<Data.Equipment> pendingCreates)
    {
        if (lookup.TryGetValue(catalogName, out var existing))
            return (existing, false);

        var created = new Data.Equipment
        {
            Name = catalogName,
            CreatedAt = DateTime.UtcNow,
        };
        lookup[catalogName] = created;
        pendingCreates.Add(created);
        return (created, true);
    }

    public static string BuildImportSummary(int importedCount, int autoCreatedCount)
    {
        var lines = new List<string>
        {
            $"Загружено: {importedCount} элементов",
        };

        if (autoCreatedCount > 0)
        {
            lines.Add(
                $"Не найдено в справочнике: {autoCreatedCount} элементов (требуется классификация)");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
