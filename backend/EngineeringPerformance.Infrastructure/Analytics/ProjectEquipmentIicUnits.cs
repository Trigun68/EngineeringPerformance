using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>Количество оборудования в единицах группы IIC: Quantity × Coefficient.</summary>
public static class ProjectEquipmentIicUnits
{
    public static decimal QuantityInIicGroupUnits(this ProjectEquipment row)
    {
        var k = row.Equipment?.Coefficient ?? 1m;
        if (k <= 0m)
        {
            k = 1m;
        }

        return row.Quantity * k;
    }
}
