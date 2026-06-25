using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Directories;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Сопоставление строк импорта оборудования со справочником по тандему (наименование + ед. изм.).</summary>
public static class EquipmentImportCatalogMatcher
{
    public readonly record struct EquipmentCatalogKey(string Name, int? UnitId);

    public sealed record ResolveResult(Equipment Equipment, bool Created, bool SkippedDuplicate);

    public static string NormalizeUnitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    public static EquipmentCatalogKey CreateKey(string name, int? unitId)
        => new(name, unitId);

    public static (
        Dictionary<EquipmentCatalogKey, Equipment> Lookup,
        HashSet<EquipmentCatalogKey> AmbiguousKeys,
        int DuplicateKeyCount) BuildCatalogLookup(
        IEnumerable<Equipment> items,
        ILogger? logger = null)
    {
        var grouped = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Name))
            .GroupBy(i => CreateKey(i.Name, i.UnitId))
            .ToList();

        var lookup = new Dictionary<EquipmentCatalogKey, Equipment>();
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
                    "Equipment catalog duplicate key Name='{Name}', UnitId={UnitId}: {Count} rows",
                    group.Key.Name,
                    group.Key.UnitId,
                    list.Count);
            }

            lookup[group.Key] = PickBestCandidate(list);
        }

        return (lookup, ambiguousKeys, duplicateKeyCount);
    }

    public static ResolveResult ResolveOrCreate(
        string catalogName,
        int? unitId,
        Dictionary<EquipmentCatalogKey, Equipment> lookup,
        HashSet<EquipmentCatalogKey> ambiguousKeys,
        IList<Equipment> pendingCreates)
    {
        var key = CreateKey(catalogName, unitId);

        if (ambiguousKeys.Contains(key))
            return new ResolveResult(new Equipment { Name = catalogName, UnitId = unitId }, false, true);

        if (lookup.TryGetValue(key, out var existing))
            return new ResolveResult(existing, false, false);

        var created = new Equipment
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
        var lines = new List<string> { $"Загружено: {importedCount} элементов" };

        if (autoCreatedCount > 0)
            lines.Add($"Не найдено в справочнике: {autoCreatedCount} элементов (требуется классификация)");

        if (skippedDuplicateCount > 0)
        {
            lines.Add($"Пропущено из-за дубликатов в справочнике: {skippedDuplicateCount}");
            foreach (var (rowNumber, name, unit) in skippedDuplicateRows.Take(20))
            {
                var unitText = string.IsNullOrWhiteSpace(unit) ? "(без ед. изм.)" : unit;
                lines.Add($"  строка {rowNumber}: «{name}» / {unitText}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static Equipment PickBestCandidate(IReadOnlyList<Equipment> candidates) =>
        candidates.Count == 1
            ? candidates[0]
            : candidates
                .OrderBy(c => c.IsArchived)
                .ThenByDescending(c => c.ClassifierId.HasValue)
                .ThenBy(c => c.Id)
                .First();
}
