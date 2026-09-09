using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.LaborIntensityServices;

/// <summary>
/// As-of резолв группы ИИС из регистра привязок (не слепок PLI.WorkDirectionId).
/// </summary>
public static class LaborIntensityIicAsOf
{
    public static async Task<LaborIntensityIicAsOfSlice> LoadAsync(
        AppDbContext context,
        DateTime asOf,
        IEnumerable<int> laborIntensityIds,
        CancellationToken cancellationToken)
    {
        var date = DateTime.SpecifyKind(asOf.Date, DateTimeKind.Utc);
        var ids = laborIntensityIds.Where(id => id > 0).Distinct().ToList();

        var claimed = await context.LaborIntensityIicBindings
            .AsNoTracking()
            .Where(b => b.IicWorkDirectionId != null
                && b.EffectiveFrom <= date
                && (b.EffectiveTo == null || b.EffectiveTo >= date))
            .Select(b => b.IicWorkDirectionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<int, int?> byLabor = [];
        if (ids.Count > 0)
        {
            var rows = await context.LaborIntensityIicBindings
                .AsNoTracking()
                .Where(b => ids.Contains(b.LaborIntensityId)
                    && b.EffectiveFrom <= date
                    && (b.EffectiveTo == null || b.EffectiveTo >= date))
                .OrderByDescending(b => b.EffectiveFrom)
                .ThenByDescending(b => b.Id)
                .Select(b => new { b.LaborIntensityId, b.IicWorkDirectionId })
                .ToListAsync(cancellationToken);

            byLabor = rows
                .GroupBy(r => r.LaborIntensityId)
                .ToDictionary(g => g.Key, g => g.First().IicWorkDirectionId);
        }

        return new LaborIntensityIicAsOfSlice(byLabor, claimed.ToHashSet());
    }

    public static void ApplyToLaborRows(
        IEnumerable<ProjectLaborIntensity> rows,
        LaborIntensityIicAsOfSlice slice)
    {
        foreach (var row in rows)
        {
            if (row.LaborIntensityId is int laborId && slice.ByLaborId.TryGetValue(laborId, out var directionId))
                row.WorkDirectionId = directionId;
            else if (row.LaborIntensityId is int)
                row.WorkDirectionId = null;
        }
    }
}

public sealed record LaborIntensityIicAsOfSlice(
    IReadOnlyDictionary<int, int?> ByLaborId,
    IReadOnlySet<int> ClaimedWorkDirectionIds);
