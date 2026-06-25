using System.Linq.Expressions;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Projects;

internal static class ProjectDataSort
{
    public static IQueryable<ProjectPhysicalVolume> Apply(
        IQueryable<ProjectPhysicalVolume> query,
        string? sort)
    {
        var (field, desc) = Parse(sort);
        return field switch
        {
            "quantity" => Order(query, x => x.Quantity, desc),
            "facilitycode" => Order(query, x => x.FacilityCode ?? string.Empty, desc),
            "facilityname" => Order(query, x => x.FacilityName ?? string.Empty, desc),
            "workname" => Order(query, x => x.WorkName ?? string.Empty, desc),
            _ => Order(query, x => x.Id, desc),
        };
    }

    public static IQueryable<ProjectLaborIntensity> Apply(
        IQueryable<ProjectLaborIntensity> query,
        string? sort)
    {
        var (field, desc) = Parse(sort);
        return field switch
        {
            "plannedhours" => Order(query, x => x.PlannedHours, desc),
            "actualhours" => Order(query, x => x.ActualHours, desc),
            _ => Order(query, x => x.Id, desc),
        };
    }

    public static IQueryable<ProjectEquipment> Apply(
        IQueryable<ProjectEquipment> query,
        string? sort)
    {
        var (field, desc) = Parse(sort);
        return field switch
        {
            "quantity" => Order(query, x => x.Quantity, desc),
            "quantityinwork" => Order(query, x => x.QuantityInWork ?? 0m, desc),
            _ => Order(query, x => x.Id, desc),
        };
    }

    private static (string Field, bool Desc) Parse(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
            return ("id", false);

        var parts = sort.Split(':', 2, StringSplitOptions.TrimEntries);
        var field = parts[0].ToLowerInvariant();
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
        return (field, desc);
    }

    private static IQueryable<T> Order<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> key, bool desc) =>
        desc ? query.OrderByDescending(key) : query.OrderBy(key);
}
