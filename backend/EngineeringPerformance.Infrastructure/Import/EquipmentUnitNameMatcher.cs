using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Сопоставление текста «Размерность» из Excel со справочником единиц измерения.</summary>
public static class EquipmentUnitNameMatcher
{
    public static int? ResolveUnitId(string? rawUnitName, IReadOnlyList<UnitOfMeasurement> units)
    {
        var token = EquipmentImportCatalogMatcher.NormalizeUnitToken(rawUnitName);
        if (string.IsNullOrEmpty(token))
            return null;

        foreach (var unit in units)
        {
            if (EquipmentImportCatalogMatcher.NormalizeUnitToken(unit.Name) == token)
                return unit.Id;
        }

        return null;
    }

    public static string? ResolveUnitDisplayName(string? rawUnitName, IReadOnlyList<UnitOfMeasurement> units)
    {
        var unitId = ResolveUnitId(rawUnitName, units);
        if (!unitId.HasValue)
            return rawUnitName?.Trim();

        return units.FirstOrDefault(u => u.Id == unitId.Value)?.Name ?? rawUnitName?.Trim();
    }
}
