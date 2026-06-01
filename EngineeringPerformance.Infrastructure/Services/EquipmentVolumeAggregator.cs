using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>Суммирует объёмы оборудования по группам IIC для расчёта трудоёмкости.</summary>
public static class EquipmentVolumeAggregator
{
    public static void MergeInto(
        Dictionary<int, decimal> volumeByDirection,
        IEnumerable<ProjectEquipment> equipmentRows)
    {
        foreach (var group in equipmentRows
                     .Where(e => e.Equipment?.ClassifierId.HasValue == true
                              && e.Equipment.IsAssessment == true)
                     .GroupBy(e => e.Equipment!.ClassifierId!.Value))
        {
            var sum = group.Sum(e => e.QuantityInIicGroupUnits());
            if (volumeByDirection.TryGetValue(group.Key, out var existing))
                volumeByDirection[group.Key] = existing + sum;
            else
                volumeByDirection[group.Key] = sum;
        }
    }

    public static decimal SumUnallocatedAssessmentVolume(IEnumerable<ProjectEquipment> equipmentRows)
    {
        return equipmentRows
            .Where(e => e.Equipment is not null
                     && !e.Equipment.ClassifierId.HasValue
                     && e.Equipment.IsAssessment == true)
            .Sum(e => e.QuantityInIicGroupUnits());
    }
}
