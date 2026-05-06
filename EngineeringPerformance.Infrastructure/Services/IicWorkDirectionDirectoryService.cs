using System.Diagnostics;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class IicWorkDirectionDirectoryService
    : DirectoryServiceBase<IicWorkDirection>, IIicWorkDirectionLookupService
{
    public IicWorkDirectionDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<IicWorkDirection> GetDbSet(AppDbContext context) => context.IicWorkDirections;

    public override async Task<IReadOnlyList<IicWorkDirection>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        IQueryable<IicWorkDirection> query = GetDbSet(context)
            .AsNoTracking()
            .Include(d => d.Unit);

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await query.ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] IicWorkDirection.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    public async Task<IReadOnlyList<IicWorkDirectionLookupItem>> GetLookupAsync(
        bool includeArchived = false,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.IicWorkDirections.AsNoTracking();

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var all = await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .Select(e => new IicWorkDirectionLookupItem(
                e.Id,
                e.ParentId,
                e.Code,
                e.Name))
            .ToListAsync(cancellationToken);

        if (!excludeId.HasValue)
            return all;

        var excluded = BuildExcludedIds(all, excludeId.Value);
        return all.Where(item => !excluded.Contains(item.Id)).ToList();
    }

    /// <summary>
    /// Возвращает множество идентификаторов редактируемого элемента и всех его
    /// потомков — эти элементы недопустимы в качестве родителя, иначе получим
    /// цикл в иерархии.
    /// </summary>
    private static HashSet<int> BuildExcludedIds(
        IReadOnlyList<IicWorkDirectionLookupItem> items,
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
