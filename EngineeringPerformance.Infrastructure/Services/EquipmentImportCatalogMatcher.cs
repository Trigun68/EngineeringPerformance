using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>Сопоставление строк импорта оборудования со справочником по тандему (наименование + ед. изм.).</summary>
public static class EquipmentImportCatalogMatcher
{
    public readonly record struct EquipmentCatalogKey(string Name, int? UnitId);

    public sealed record ResolveResult(Data.Equipment Equipment, bool Created, bool SkippedDuplicate);

    /// <summary>Нормализованный ключ для сравнения ед. изм. из Excel и справочника.</summary>
    public static string NormalizeUnitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    public static EquipmentCatalogKey CreateKey(string name, int? unitId)
        => new(name, unitId);

    /// <summary>
    /// Строит lookup по (Name, UnitId). При нескольких строках каталога с одним ключом
    /// выбирается лучший кандидат, а ключ помечается как неоднозначный.
    /// </summary>
    public static (
        Dictionary<EquipmentCatalogKey, Data.Equipment> Lookup,
        HashSet<EquipmentCatalogKey> AmbiguousKeys,
        int DuplicateKeyCount) BuildCatalogLookup(
        IEnumerable<Data.Equipment> items,
        ILogger? logger = null)
    {
        var grouped = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .GroupBy(i => CreateKey(i.Name, i.UnitId))
            .ToList();

        var lookup = new Dictionary<EquipmentCatalogKey, Data.Equipment>();
        var ambiguousKeys = new HashSet<EquipmentCatalogKey>();
        var duplicateKeyCount = 0;

        foreach (var group in grouped)
        {
            var list = group.ToList();
            if (list.Count > 1)
            {
                duplicateKeyCount++;
                ambiguousKeys.Add(group.Key);
                logger?.LogWarning(
                    "Equipment catalog duplicate key Name='{Name}', UnitId={UnitId}: {Count} rows; import rows with this key will be skipped",
                    group.Key.Name,
                    group.Key.UnitId,
                    list.Count);
            }

            lookup[group.Key] = PickBestCandidate(list);
        }

        if (duplicateKeyCount > 0)
        {
            logger?.LogInformation(
                "Equipment catalog: {Count} keys map to multiple rows (Name + UnitId); matching import rows will be skipped",
                duplicateKeyCount);
        }

        return (lookup, ambiguousKeys, duplicateKeyCount);
    }

    /// <summary>
    /// Находит элемент по (наименование + ед. изм.) или создаёт новый с дефолтами.
    /// </summary>
    public static ResolveResult ResolveOrCreate(
        string catalogName,
        int? unitId,
        Dictionary<EquipmentCatalogKey, Data.Equipment> lookup,
        HashSet<EquipmentCatalogKey> ambiguousKeys,
        IList<Data.Equipment> pendingCreates)
    {
        var key = CreateKey(catalogName, unitId);

        if (ambiguousKeys.Contains(key))
            return new ResolveResult(new Data.Equipment { Name = catalogName, UnitId = unitId }, false, true);

        if (lookup.TryGetValue(key, out var existing))
            return new ResolveResult(existing, false, false);

        var created = new Data.Equipment
        {
            Name = catalogName,
            UnitId = unitId,
            CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(catalogName, unitId),
            Coefficient = 1m,
            IsAssessment = false,
            IsArchived = false,
            ClassifierId = null,
            CreatedAt = DateTime.UtcNow,
        };
        lookup[key] = created;
        pendingCreates.Add(created);
        return new ResolveResult(created, true, false);
    }

    public static string BuildImportSummary(
        int importedCount,
        int autoCreatedCount,
        int skippedDuplicateCount,
        IReadOnlyList<(int RowNumber, string Name, string Unit)> skippedDuplicateRows)
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

        if (skippedDuplicateCount > 0)
        {
            lines.Add(
                $"Пропущено из-за дубликатов в справочнике (наименование + ед. изм.): {skippedDuplicateCount}");
            const int maxRows = 20;
            foreach (var (rowNumber, name, unit) in skippedDuplicateRows.Take(maxRows))
            {
                var unitText = string.IsNullOrWhiteSpace(unit) ? "(без ед. изм.)" : unit;
                lines.Add($"  строка {rowNumber}: «{name}» / {unitText}");
            }

            if (skippedDuplicateRows.Count > maxRows)
            {
                lines.Add($"  … и ещё {skippedDuplicateRows.Count - maxRows} строк (см. лог приложения).");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static Data.Equipment PickBestCandidate(IReadOnlyList<Data.Equipment> candidates)
    {
        if (candidates.Count == 1)
            return candidates[0];

        return candidates
            .OrderBy(c => c.IsArchived)
            .ThenByDescending(c => c.ClassifierId.HasValue)
            .ThenBy(c => c.Id)
            .First();
    }
}
