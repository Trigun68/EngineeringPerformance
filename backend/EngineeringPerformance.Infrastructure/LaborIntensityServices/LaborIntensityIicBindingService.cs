using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.LaborIntensityServices;

public sealed class LaborIntensityIicBindingService(
    AppDbContext context,
    IUserActionLogger actionLogger,
    IProjectAnalyticsCache analyticsCache,
    IFactNormCacheGeneration factNormCache) : ILaborIntensityIicBindingService
{
    public async Task<IReadOnlyList<LaborIntensityIicBindingDto>> ListAsync(
        int laborIntensityId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.LaborIntensityIicBindings
            .AsNoTracking()
            .Where(b => b.LaborIntensityId == laborIntensityId)
            .OrderByDescending(b => b.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var names = await LoadGroupNamesAsync(
            rows.Select(r => r.IicWorkDirectionId).Where(id => id.HasValue).Select(id => id!.Value),
            cancellationToken);

        return rows.Select(r => Map(r, names)).ToList();
    }

    public async Task<LaborIntensityIicBindingDto?> CreateAsync(
        int laborIntensityId,
        LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await context.LaborIntensities.AnyAsync(x => x.Id == laborIntensityId, cancellationToken))
            return null;

        var effectiveFrom = DateOnlyUtc(request.EffectiveFrom);
        await EnsureDirectionExistsAsync(request.IicWorkDirectionId, cancellationToken);

        var existing = await context.LaborIntensityIicBindings
            .Where(b => b.LaborIntensityId == laborIntensityId)
            .ToListAsync(cancellationToken);

        var sameFrom = existing.FirstOrDefault(b => b.EffectiveFrom.Date == effectiveFrom.Date);
        if (sameFrom is not null)
        {
            sameFrom.IicWorkDirectionId = request.IicWorkDirectionId;
            sameFrom.ModifiedAt = DateTime.UtcNow;
            await SyncCatalogCurrentAsync(laborIntensityId, existing, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await InvalidateAsync(cancellationToken);
            return await MapOneAsync(sameFrom, cancellationToken);
        }

        var insertedTo = ResolveInsertedEffectiveTo(existing, effectiveFrom);
        AutoClosePrevious(existing, effectiveFrom, DateTime.UtcNow);

        var entity = new LaborIntensityIicBinding
        {
            LaborIntensityId = laborIntensityId,
            IicWorkDirectionId = request.IicWorkDirectionId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = insertedTo,
            CreatedAt = DateTime.UtcNow,
        };
        context.LaborIntensityIicBindings.Add(entity);
        existing.Add(entity);
        await SyncCatalogCurrentAsync(laborIntensityId, existing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await actionLogger.LogAsync(
            UserActionType.LaborIntensityEdit,
            entityType: nameof(LaborIntensityIicBinding),
            entityId: entity.Id,
            description: $"LaborIntensityId={laborIntensityId} from={effectiveFrom:yyyy-MM-dd}",
            cancellationToken: cancellationToken);
        await InvalidateAsync(cancellationToken);
        return await MapOneAsync(entity, cancellationToken);
    }

    public async Task<LaborIntensityIicBindingDto?> UpdateAsync(
        int id,
        LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.LaborIntensityIicBindings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var effectiveFrom = DateOnlyUtc(request.EffectiveFrom);
        await EnsureDirectionExistsAsync(request.IicWorkDirectionId, cancellationToken);

        var siblings = await context.LaborIntensityIicBindings
            .Where(b => b.LaborIntensityId == entity.LaborIntensityId)
            .ToListAsync(cancellationToken);

        var conflict = siblings.FirstOrDefault(b =>
            b.Id != id && b.EffectiveFrom.Date == effectiveFrom.Date);
        if (conflict is not null)
            throw new InvalidOperationException("На эту дату уже есть запись привязки.");

        entity.IicWorkDirectionId = request.IicWorkDirectionId;
        entity.EffectiveFrom = effectiveFrom;
        entity.ModifiedAt = DateTime.UtcNow;

        RebuildPeriods(siblings, DateTime.UtcNow);
        await SyncCatalogCurrentAsync(entity.LaborIntensityId, siblings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(cancellationToken);
        return await MapOneAsync(entity, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await context.LaborIntensityIicBindings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity is null)
            return false;

        var siblings = await context.LaborIntensityIicBindings
            .Where(b => b.LaborIntensityId == entity.LaborIntensityId)
            .ToListAsync(cancellationToken);

        var previous = siblings
            .Where(b => b.Id != entity.Id && b.EffectiveFrom < entity.EffectiveFrom)
            .OrderByDescending(b => b.EffectiveFrom)
            .FirstOrDefault();
        if (previous is not null)
        {
            previous.EffectiveTo = entity.EffectiveTo;
            previous.ModifiedAt = DateTime.UtcNow;
        }

        context.LaborIntensityIicBindings.Remove(entity);
        siblings.Remove(entity);
        await SyncCatalogCurrentAsync(entity.LaborIntensityId, siblings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await InvalidateAsync(cancellationToken);
        return true;
    }

    private async Task<LaborIntensityIicBindingDto> MapOneAsync(
        LaborIntensityIicBinding entity,
        CancellationToken cancellationToken)
    {
        var names = await LoadGroupNamesAsync(
            entity.IicWorkDirectionId is int dir ? [dir] : [],
            cancellationToken);
        return Map(entity, names);
    }

    private async Task<IReadOnlyDictionary<int, string>> LoadGroupNamesAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<int, string>();

        var directions = await context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .ToListAsync(cancellationToken);

        var map = directions.ToDictionary(d => d.Id);
        var result = new Dictionary<int, string>();
        foreach (var id in idList)
        {
            if (map.TryGetValue(id, out var dir))
                result[id] = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(dir, map);
        }

        return result;
    }

    private static LaborIntensityIicBindingDto Map(
        LaborIntensityIicBinding entity,
        IReadOnlyDictionary<int, string> names)
    {
        string? name = null;
        if (entity.IicWorkDirectionId is int dir && names.TryGetValue(dir, out var formatted))
            name = formatted;

        return new LaborIntensityIicBindingDto(
            entity.Id,
            entity.LaborIntensityId,
            entity.IicWorkDirectionId,
            name,
            entity.EffectiveFrom,
            entity.EffectiveTo);
    }

    private async Task EnsureDirectionExistsAsync(int? directionId, CancellationToken cancellationToken)
    {
        if (directionId is not int id)
            return;

        if (!await context.IicWorkDirections.AnyAsync(d => d.Id == id, cancellationToken))
            throw new InvalidOperationException("Группа ИИС не найдена.");
    }

    private async Task SyncCatalogCurrentAsync(
        int laborIntensityId,
        IReadOnlyList<LaborIntensityIicBinding> bindings,
        CancellationToken cancellationToken)
    {
        var catalog = await context.LaborIntensities
            .FirstOrDefaultAsync(x => x.Id == laborIntensityId, cancellationToken);
        if (catalog is null)
            return;

        catalog.IicWorkDirectionId = ResolveAsOf(bindings, DateTime.UtcNow);
    }

    private async Task InvalidateAsync(CancellationToken cancellationToken)
    {
        await analyticsCache.InvalidateAllAsync(cancellationToken);
        await factNormCache.BumpAsync(cancellationToken);
    }

    internal static int? ResolveAsOf(IEnumerable<LaborIntensityIicBinding> bindings, DateTime asOf)
    {
        var date = DateOnlyUtc(asOf);
        var match = bindings
            .Where(b => b.EffectiveFrom.Date <= date.Date
                && (b.EffectiveTo == null || b.EffectiveTo.Value.Date >= date.Date))
            .OrderByDescending(b => b.EffectiveFrom)
            .FirstOrDefault();
        return match?.IicWorkDirectionId;
    }

    internal static void AutoClosePrevious(
        IReadOnlyList<LaborIntensityIicBinding> existing,
        DateTime newEffectiveFrom,
        DateTime now)
    {
        var closeDate = DateOnlyUtc(newEffectiveFrom.AddDays(-1));
        foreach (var row in existing)
        {
            if (row.EffectiveFrom.Date >= newEffectiveFrom.Date)
                continue;

            if (row.EffectiveTo is null || row.EffectiveTo.Value.Date >= newEffectiveFrom.Date)
            {
                row.EffectiveTo = closeDate;
                row.ModifiedAt = now;
            }
        }
    }

    internal static DateTime? ResolveInsertedEffectiveTo(
        IReadOnlyList<LaborIntensityIicBinding> existing,
        DateTime effectiveFrom)
    {
        var next = existing
            .Where(b => b.EffectiveFrom.Date > effectiveFrom.Date)
            .OrderBy(b => b.EffectiveFrom)
            .FirstOrDefault();
        if (next is null)
            return null;

        return DateOnlyUtc(next.EffectiveFrom.AddDays(-1));
    }

    /// <summary>Пересобирает концы периодов по возрастанию EffectiveFrom без пересечений.</summary>
    internal static void RebuildPeriods(IReadOnlyList<LaborIntensityIicBinding> bindings, DateTime now)
    {
        var ordered = bindings.OrderBy(b => b.EffectiveFrom).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var nextFrom = i + 1 < ordered.Count ? ordered[i + 1].EffectiveFrom : (DateTime?)null;
            var expectedTo = nextFrom is DateTime next
                ? DateOnlyUtc(next.AddDays(-1))
                : (DateTime?)null;
            if (ordered[i].EffectiveTo != expectedTo)
            {
                ordered[i].EffectiveTo = expectedTo;
                ordered[i].ModifiedAt = now;
            }
        }
    }

    private static DateTime DateOnlyUtc(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
