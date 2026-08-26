using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Directories;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Сопоставление строк импорта оборудования со справочником проекта
/// (Name + UnitId) с копированием признаков из других проектов.
/// </summary>
public static class EquipmentImportCatalogMatcher
{
    public readonly record struct EquipmentCatalogKey(string Name, int? UnitId);

    public sealed record ResolveResult(Equipment Equipment, bool Created, bool SkippedDuplicate, bool CopiedFromOtherProject);

    public static string NormalizeUnitToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    public static EquipmentCatalogKey CreateKey(string name, int? unitId)
        => new(name, unitId);

    /// <summary>
    /// Lookup только по строкам одного проекта (ProjectId == projectId).
    /// Ambiguous — несколько активных с одним ключом внутри проекта.
    /// </summary>
    public static (
        Dictionary<EquipmentCatalogKey, Equipment> Lookup,
        HashSet<EquipmentCatalogKey> AmbiguousKeys,
        int DuplicateKeyCount) BuildCatalogLookup(
        IEnumerable<Equipment> items,
        int projectId,
        ILogger? logger = null)
    {
        var scoped = items
            .Where(i => i.ProjectId == projectId && !string.IsNullOrWhiteSpace(i.Name))
            .ToList();

        return BuildLookupForItems(scoped, logger);
    }

    /// <summary>Lookup без фильтра проекта (тесты / legacy). Группирует по (Name, UnitId) глобально.</summary>
    public static (
        Dictionary<EquipmentCatalogKey, Equipment> Lookup,
        HashSet<EquipmentCatalogKey> AmbiguousKeys,
        int DuplicateKeyCount) BuildCatalogLookup(
        IEnumerable<Equipment> items,
        ILogger? logger = null)
        => BuildLookupForItems(
            items.Where(i => !string.IsNullOrWhiteSpace(i.Name)),
            logger);

    private static (
        Dictionary<EquipmentCatalogKey, Equipment> Lookup,
        HashSet<EquipmentCatalogKey> AmbiguousKeys,
        int DuplicateKeyCount) BuildLookupForItems(
        IEnumerable<Equipment> items,
        ILogger? logger)
    {
        var grouped = items
            .GroupBy(i => CreateKey(i.Name, i.UnitId))
            .ToList();

        var lookup = new Dictionary<EquipmentCatalogKey, Equipment>();
        var ambiguousKeys = new HashSet<EquipmentCatalogKey>();
        var duplicateKeyCount = 0;

        foreach (var group in grouped)
        {
            var list = group.ToList();
            var active = list.Where(c => !c.IsArchived).ToList();

            if (active.Count == 1)
            {
                lookup[group.Key] = active[0];
                if (list.Count > 1)
                {
                    logger?.LogInformation(
                        "Equipment catalog key Name='{Name}', UnitId={UnitId}: using active Id={Id}, ignoring {ArchivedCount} archived twin(s)",
                        group.Key.Name,
                        group.Key.UnitId,
                        active[0].Id,
                        list.Count - 1);
                }

                continue;
            }

            if (active.Count > 1)
            {
                duplicateKeyCount++;
                ambiguousKeys.Add(group.Key);
                lookup[group.Key] = PickBestCandidate(active);
                logger?.LogWarning(
                    "Equipment catalog duplicate active key Name='{Name}', UnitId={UnitId}: {Count} rows",
                    group.Key.Name,
                    group.Key.UnitId,
                    active.Count);
                continue;
            }

            if (list.Count > 1)
            {
                duplicateKeyCount++;
                ambiguousKeys.Add(group.Key);
                logger?.LogWarning(
                    "Equipment catalog duplicate archived key Name='{Name}', UnitId={UnitId}: {Count} rows",
                    group.Key.Name,
                    group.Key.UnitId,
                    list.Count);
            }

            lookup[group.Key] = PickBestCandidate(list);
        }

        return (lookup, ambiguousKeys, duplicateKeyCount);
    }

    /// <summary>
    /// 1) Есть в текущем проекте → reuse.
    /// 2) Есть в другом проекте → копия признаков (донор: max ModifiedAt, затем min Id).
    /// 3) Иначе stub (Coefficient=1, IsAssessment=false).
    /// </summary>
    public static ResolveResult ResolveOrCreate(
        int projectId,
        string catalogName,
        int? unitId,
        Dictionary<EquipmentCatalogKey, Equipment> projectLookup,
        HashSet<EquipmentCatalogKey> ambiguousKeys,
        IReadOnlyList<Equipment> allCatalogRows,
        IList<Equipment> pendingCreates)
    {
        var key = CreateKey(catalogName, unitId);

        if (ambiguousKeys.Contains(key))
            return new ResolveResult(new Equipment { Name = catalogName, UnitId = unitId, ProjectId = projectId }, false, true, false);

        if (projectLookup.TryGetValue(key, out var existing))
            return new ResolveResult(existing, false, false, false);

        var donor = FindDonor(projectId, catalogName, unitId, allCatalogRows, pendingCreates);
        Equipment created;
        var copied = donor is not null;

        if (donor is not null)
        {
            created = new Equipment
            {
                Name = catalogName,
                UnitId = unitId,
                ProjectId = projectId,
                CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(projectId, catalogName, unitId),
                Coefficient = donor.Coefficient,
                IsAssessment = donor.IsAssessment,
                ClassifierId = donor.ClassifierId,
                ParentId = null,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
        }
        else
        {
            created = new Equipment
            {
                Name = catalogName,
                UnitId = unitId,
                ProjectId = projectId,
                CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(projectId, catalogName, unitId),
                Coefficient = 1m,
                IsAssessment = false,
                ClassifierId = null,
                ParentId = null,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
        }

        projectLookup[key] = created;
        pendingCreates.Add(created);
        return new ResolveResult(created, true, false, copied);
    }

    /// <summary>Legacy overload без projectId — создаёт stub с ProjectId=null (тесты).</summary>
    public static ResolveResult ResolveOrCreate(
        string catalogName,
        int? unitId,
        Dictionary<EquipmentCatalogKey, Equipment> lookup,
        HashSet<EquipmentCatalogKey> ambiguousKeys,
        IList<Equipment> pendingCreates)
    {
        var key = CreateKey(catalogName, unitId);

        if (ambiguousKeys.Contains(key))
            return new ResolveResult(new Equipment { Name = catalogName, UnitId = unitId }, false, true, false);

        if (lookup.TryGetValue(key, out var existing))
            return new ResolveResult(existing, false, false, false);

        var created = new Equipment
        {
            Name = catalogName,
            UnitId = unitId,
            CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(projectId: null, catalogName, unitId),
            Coefficient = 1m,
            IsAssessment = false,
            IsArchived = false,
            ClassifierId = null,
            CreatedAt = DateTime.UtcNow,
        };
        lookup[key] = created;
        pendingCreates.Add(created);
        return new ResolveResult(created, true, false, false);
    }

    public static string BuildImportSummary(
        int importedCount,
        int autoCreatedCount,
        int skippedDuplicateCount,
        IReadOnlyList<(int RowNumber, string Name, string Unit)> skippedDuplicateRows,
        int copiedFromOtherProjectCount = 0)
    {
        var lines = new List<string> { $"Загружено: {importedCount} элементов" };

        if (copiedFromOtherProjectCount > 0)
            lines.Add($"Скопировано из других проектов: {copiedFromOtherProjectCount}");

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

    private static Equipment? FindDonor(
        int projectId,
        string catalogName,
        int? unitId,
        IReadOnlyList<Equipment> allCatalogRows,
        IList<Equipment> pendingCreates)
    {
        var candidates = allCatalogRows
            .Where(e =>
                e.ProjectId is int pid
                && pid != projectId
                && !e.IsArchived
                && string.Equals(e.Name, catalogName, StringComparison.Ordinal)
                && e.UnitId == unitId)
            .Concat(pendingCreates.Where(e =>
                e.ProjectId is int pid
                && pid != projectId
                && !e.IsArchived
                && string.Equals(e.Name, catalogName, StringComparison.Ordinal)
                && e.UnitId == unitId))
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(e => e.ModifiedAt ?? e.CreatedAt)
            .ThenBy(e => e.Id)
            .First();
    }

    private static Equipment PickBestCandidate(IReadOnlyList<Equipment> candidates) =>
        candidates.Count == 1
            ? candidates[0]
            : candidates
                .OrderBy(c => c.IsArchived)
                .ThenByDescending(c => c.IsAssessment)
                .ThenByDescending(c => c.ClassifierId.HasValue)
                .ThenBy(c => c.Id)
                .First();
}
