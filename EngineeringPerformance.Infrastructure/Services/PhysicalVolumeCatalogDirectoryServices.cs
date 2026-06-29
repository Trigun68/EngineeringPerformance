using System.Diagnostics;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public abstract class PhysicalVolumeCatalogDirectoryServiceBase<T> : DirectoryServiceBase<T>, IPhysicalVolumeLookupService
    where T : DirectoryItem, new()
{
    protected PhysicalVolumeCatalogDirectoryServiceBase(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    public override async Task<IReadOnlyList<T>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        IQueryable<T> query = GetDbSet(context).AsNoTracking();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await ProjectListAsync(query);
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] {Type}.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            typeof(T).Name, includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    protected abstract Task<List<T>> ProjectListAsync(IQueryable<T> query);

    public override async Task UpdateAsync(T item)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        item.ModifiedAt = DateTime.UtcNow;
        await ApplyUpdateAsync(context, item);
    }

    protected abstract Task ApplyUpdateAsync(AppDbContext context, T item);

    public async Task<IReadOnlyList<PhysicalVolumeLookupItem>> GetLookupAsync(
        bool includeArchived = false,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var query = GetDbSet(context).AsNoTracking();

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var all = await ProjectLookupAsync(query, cancellationToken);

        if (!excludeId.HasValue)
            return all;

        var excluded = BuildExcludedIds(all, excludeId.Value);
        return all.Where(item => !excluded.Contains(item.Id)).ToList();
    }

    protected abstract Task<IReadOnlyList<PhysicalVolumeLookupItem>> ProjectLookupAsync(
        IQueryable<T> query,
        CancellationToken cancellationToken);

    private static HashSet<int> BuildExcludedIds(
        IReadOnlyList<PhysicalVolumeLookupItem> items,
        int rootId)
    {
        var childrenByParent = items
            .Where(i => i.ParentId.HasValue)
            .ToLookup(i => i.ParentId!.Value, i => i.Id);

        var excluded = new HashSet<int> { rootId };
        var stack = new Stack<int>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var childId in childrenByParent[current])
            {
                if (excluded.Add(childId))
                    stack.Push(childId);
            }
        }

        return excluded;
    }
}

public sealed class PhysicalVolumeKfoDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
    : PhysicalVolumeCatalogDirectoryServiceBase<PhysicalVolumeKfo>(contextFactory)
{
    protected override DbSet<PhysicalVolumeKfo> GetDbSet(AppDbContext context) => context.PhysicalVolumesKfo;

    protected override Task<List<PhysicalVolumeKfo>> ProjectListAsync(IQueryable<PhysicalVolumeKfo> query) =>
        query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .Select(e => new PhysicalVolumeKfo
            {
                Id = e.Id,
                ParentId = e.ParentId,
                SortOrder = e.SortOrder,
                IsArchived = e.IsArchived,
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                ClassifierId = e.ClassifierId,
                UnitId = e.UnitId,
                IsAssessment = e.IsAssessment,
                Coefficient = e.Coefficient,
            })
            .ToListAsync();

    protected override async Task ApplyUpdateAsync(AppDbContext context, PhysicalVolumeKfo item)
    {
        var affected = await context.PhysicalVolumesKfo
            .Where(e => e.Id == item.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Code, item.Code)
                .SetProperty(e => e.Name, item.Name)
                .SetProperty(e => e.Description, item.Description)
                .SetProperty(e => e.ClassifierId, item.ClassifierId)
                .SetProperty(e => e.ParentId, item.ParentId)
                .SetProperty(e => e.UnitId, item.UnitId)
                .SetProperty(e => e.IsAssessment, item.IsAssessment)
                .SetProperty(e => e.Coefficient, item.Coefficient)
                .SetProperty(e => e.DocumentSet, item.DocumentSet)
                .SetProperty(e => e.ModifiedAt, item.ModifiedAt));

        if (affected == 0)
            throw new InvalidOperationException($"PhysicalVolumeKfo with Id={item.Id} not found.");
    }

    protected override async Task<IReadOnlyList<PhysicalVolumeLookupItem>> ProjectLookupAsync(
        IQueryable<PhysicalVolumeKfo> query,
        CancellationToken cancellationToken) =>
        await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .Select(e => new PhysicalVolumeLookupItem(e.Id, e.ParentId, e.Code, e.Name))
            .ToListAsync(cancellationToken);
}

public sealed class PhysicalVolumeBerDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
    : PhysicalVolumeCatalogDirectoryServiceBase<PhysicalVolumeBer>(contextFactory)
{
    protected override DbSet<PhysicalVolumeBer> GetDbSet(AppDbContext context) => context.PhysicalVolumesBer;

    protected override Task<List<PhysicalVolumeBer>> ProjectListAsync(IQueryable<PhysicalVolumeBer> query) =>
        query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .Select(e => new PhysicalVolumeBer
            {
                Id = e.Id,
                ParentId = e.ParentId,
                SortOrder = e.SortOrder,
                IsArchived = e.IsArchived,
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                ClassifierId = e.ClassifierId,
                UnitId = e.UnitId,
                IsAssessment = e.IsAssessment,
                Coefficient = e.Coefficient,
            })
            .ToListAsync();

    protected override async Task ApplyUpdateAsync(AppDbContext context, PhysicalVolumeBer item)
    {
        var affected = await context.PhysicalVolumesBer
            .Where(e => e.Id == item.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Code, item.Code)
                .SetProperty(e => e.Name, item.Name)
                .SetProperty(e => e.Description, item.Description)
                .SetProperty(e => e.ClassifierId, item.ClassifierId)
                .SetProperty(e => e.ParentId, item.ParentId)
                .SetProperty(e => e.UnitId, item.UnitId)
                .SetProperty(e => e.IsAssessment, item.IsAssessment)
                .SetProperty(e => e.Coefficient, item.Coefficient)
                .SetProperty(e => e.DocumentSet, item.DocumentSet)
                .SetProperty(e => e.ModifiedAt, item.ModifiedAt));

        if (affected == 0)
            throw new InvalidOperationException($"PhysicalVolumeBer with Id={item.Id} not found.");
    }

    protected override async Task<IReadOnlyList<PhysicalVolumeLookupItem>> ProjectLookupAsync(
        IQueryable<PhysicalVolumeBer> query,
        CancellationToken cancellationToken) =>
        await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .Select(e => new PhysicalVolumeLookupItem(e.Id, e.ParentId, e.Code, e.Name))
            .ToListAsync(cancellationToken);
}
