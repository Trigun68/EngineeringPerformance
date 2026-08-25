using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Чтение сохранённых плановых часов (ФО + оборудование). Без N(V) и без движка.
/// </summary>
internal static class StoredPlanHours
{
    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ByPpvId(
        ProjectsPageVersionBundle bundle)
    {
        var map = new Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)>();
        foreach (var pv in bundle.PhysicalVolumes)
        {
            if (pv.GetIsAssessment() != true)
                continue;
            map[pv.Id] = (pv.LaborIntensityBase, pv.LaborIntensityOptimistic, pv.LaborIntensityPessimistic);
        }

        return map;
    }

    public static (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ForEquipment(ProjectEquipment eq) =>
        (eq.LaborIntensityBase, eq.LaborIntensityOptimistic, eq.LaborIntensityPessimistic);

    public static Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ByDirection(
        ProjectsPageVersionBundle bundle)
    {
        var byDirection = new Dictionary<int, (decimal Base, decimal Optimistic, decimal Pessimistic)>();

        void Add(int dirId, decimal? b, decimal? o, decimal? p)
        {
            if (!byDirection.TryGetValue(dirId, out var acc))
                acc = (0m, 0m, 0m);

            byDirection[dirId] = (
                acc.Base + (b ?? 0m),
                acc.Optimistic + (o ?? 0m),
                acc.Pessimistic + (p ?? 0m));
        }

        foreach (var pv in bundle.PhysicalVolumes)
        {
            if (pv.GetIsAssessment() != true || pv.GetClassifierId() is not int dir)
                continue;
            Add(dir, pv.LaborIntensityBase, pv.LaborIntensityOptimistic, pv.LaborIntensityPessimistic);
        }

        foreach (var eq in bundle.Equipment)
        {
            if (eq.Equipment?.IsAssessment != true || eq.Equipment.ClassifierId is not int dir)
                continue;
            Add(dir, eq.LaborIntensityBase, eq.LaborIntensityOptimistic, eq.LaborIntensityPessimistic);
        }

        return byDirection.ToDictionary(
            kv => kv.Key,
            kv => ((decimal?)kv.Value.Base, (decimal?)kv.Value.Optimistic, (decimal?)kv.Value.Pessimistic));
    }
}
