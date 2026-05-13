namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>
/// Единица измерения для норм и отчётов: из справочника групп ИИС (поле ед. изм. у предков),
/// без отдельного выбора единицы на норме. Сначала ищется у родителей вверх по дереву, иначе — у самой группы.
/// </summary>
public static class IicWorkDirectionDisplayUnit
{
    public static string? ResolveUnitName(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        var u = ResolveUnit(direction, byId);
        return u?.Name is string name && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;
    }

    public static int? ResolveUnitId(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
        => ResolveUnit(direction, byId)?.Id;

    public static UnitOfMeasurement? ResolveUnit(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        if (direction.ParentId is int pid && byId.TryGetValue(pid, out var parent))
        {
            var current = parent;
            while (true)
            {
                if (current.Unit is not null)
                    return current.Unit;
                if (current.ParentId is not int ppid || !byId.TryGetValue(ppid, out var next))
                    break;
                current = next;
            }
        }

        return direction.Unit;
    }

    /// <summary>
    /// Наименование группы ИИС для UI: «Имя (ед. изм.)», единица — унаследованная по дереву справочника.
    /// </summary>
    public static string FormatNameWithResolvedUnit(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        var unitName = ResolveUnitName(direction, byId);
        return string.IsNullOrWhiteSpace(unitName)
            ? direction.Name
            : $"{direction.Name} ({unitName})";
    }
}
