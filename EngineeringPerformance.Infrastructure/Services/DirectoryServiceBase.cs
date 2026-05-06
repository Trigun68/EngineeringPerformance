using System.Diagnostics;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public abstract class DirectoryServiceBase<T> : IDirectoryService<T>
    where T : DirectoryItem, new()
{
    protected readonly IDbContextFactory<AppDbContext> ContextFactory;

    protected DirectoryServiceBase(IDbContextFactory<AppDbContext> contextFactory)
    {
        ContextFactory = contextFactory;
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        var query = GetDbSet(context).AsNoTracking();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] {Type}.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            typeof(T).Name, includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await GetDbSet(context).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IReadOnlyList<T>> GetChildrenAsync(int? parentId = null, bool includeArchived = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = GetDbSet(context).AsNoTracking().Where(e => e.ParentId == parentId);

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        return await query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToListAsync();
    }

    public virtual async Task<T> CreateAsync(T item)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        item.CreatedAt = DateTime.UtcNow;
        GetDbSet(context).Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    public virtual async Task UpdateAsync(T item)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var tracked = await GetDbSet(context).FirstOrDefaultAsync(e => e.Id == item.Id);
        if (tracked is null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} with Id={item.Id} not found.");
        }

        item.ModifiedAt = DateTime.UtcNow;
        // Копируем только скалярные свойства и FK из пришедшего item в отслеживаемую копию.
        // DbSet.Update(item) ранее помечал Modified для всего графа — если в GetByIdAsync
        // было .Include(...), навигации уходили в UPDATE с устаревшими значениями из VM,
        // перетирая параллельные правки связанных сущностей (Lost Update).
        context.Entry(tracked).CurrentValues.SetValues(item);
        await context.SaveChangesAsync();
    }

    public virtual async Task ArchiveAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var entity = await GetDbSet(context).FirstOrDefaultAsync(e => e.Id == id);
        if (entity is not null)
        {
            entity.IsArchived = true;
            entity.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public virtual async Task UnarchiveAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var entity = await GetDbSet(context).FirstOrDefaultAsync(e => e.Id == id);
        if (entity is not null)
        {
            entity.IsArchived = false;
            entity.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public virtual async Task DeleteAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var entity = await GetDbSet(context).FirstOrDefaultAsync(e => e.Id == id);
        if (entity is not null)
        {
            GetDbSet(context).Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public virtual async Task UpdateSortOrderAsync(int itemId, int? newParentId, int newSortOrder)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var entity = await GetDbSet(context).FirstOrDefaultAsync(e => e.Id == itemId);
        if (entity is null) return;

        var oldParentId = entity.ParentId;
        var parentChanged = oldParentId != newParentId;

        entity.ParentId = newParentId;
        entity.SortOrder = newSortOrder;
        entity.ModifiedAt = DateTime.UtcNow;

        // Пересчитываем соседей нового родителя, вставляя entity на позицию newSortOrder.
        var newSiblings = await GetDbSet(context)
            .Where(e => e.Id != itemId && e.ParentId == newParentId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync();

        var insertIndex = 0;
        foreach (var sibling in newSiblings)
        {
            if (insertIndex == newSortOrder)
                insertIndex++;
            sibling.SortOrder = insertIndex;
            insertIndex++;
        }

        // Если сменился родитель, старые соседи получают «дыру» в нумерации.
        // Перепаковываем их в плотный 0..N-1, чтобы дальнейшие DnD-операции были
        // устойчивы, а ThenBy(SortOrder) в выборках давал детерминированный порядок.
        if (parentChanged)
        {
            var oldSiblings = await GetDbSet(context)
                .Where(e => e.Id != itemId && e.ParentId == oldParentId)
                .OrderBy(e => e.SortOrder)
                .ToListAsync();

            for (var i = 0; i < oldSiblings.Count; i++)
            {
                oldSiblings[i].SortOrder = i;
            }
        }

        await context.SaveChangesAsync();
    }

    protected abstract DbSet<T> GetDbSet(AppDbContext context);
}
