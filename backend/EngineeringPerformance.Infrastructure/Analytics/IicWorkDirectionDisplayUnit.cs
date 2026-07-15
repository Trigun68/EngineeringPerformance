using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

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
    /// Наименование группы ИИС для UI: «Имя (ед. изм.)».
    /// Для номенклатуры ФО сначала берется ед. изм. самой выбранной группы IIC,
    /// и только если она не заполнена — наследуется от предков по дереву IIC.
    /// </summary>
    public static string FormatNameWithResolvedUnit(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        var ownUnitName = direction.Unit?.Name is string ownName && !string.IsNullOrWhiteSpace(ownName)
            ? ownName.Trim()
            : null;
        var unitName = ownUnitName ?? ResolveUnitName(direction, byId);

        return string.IsNullOrWhiteSpace(unitName)
            ? direction.Name
            : $"{direction.Name} ({unitName})";
    }

    /// <summary>
    /// «Группа ФО IIC» в отчётах и гридах: код, наименование и ед. изм. группы ИИС — «код имя (ед.)».
    /// </summary>
    public static string FormatCodeNameWithResolvedUnit(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirection> byId)
    {
        var baseName = string.IsNullOrWhiteSpace(direction.Code)
            ? direction.Name
            : $"{direction.Code.Trim()} — {direction.Name}".Trim();
        var unitName = ResolveUnitName(direction, byId);
        return string.IsNullOrWhiteSpace(unitName) ? baseName : $"{baseName} ({unitName})";
    }

    /// <summary>Ед. изм. номенклатуры ФО (справочник Unit).</summary>
    public static string? ResolvePhysicalVolumeUnitName(PhysicalVolumeKfo? volume) =>
        volume?.Unit?.Name is string name && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;

    public static string? ResolvePhysicalVolumeUnitName(PhysicalVolumeBer? volume) =>
        volume?.Unit?.Name is string name && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;

    [Obsolete("Use PhysicalVolumeKfo or PhysicalVolumeBer overload.")]
    public static string? ResolvePhysicalVolumeUnitName(PhysicalVolume? volume) =>
        volume?.Unit?.Name is string name && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;

    public static string? ResolveProjectPhysicalVolumeUnitName(ProjectPhysicalVolume ppv) =>
        ResolvePhysicalVolumeUnitName(ppv.PhysicalVolumeKfo) ?? ResolvePhysicalVolumeUnitName(ppv.PhysicalVolumeBer);
}
