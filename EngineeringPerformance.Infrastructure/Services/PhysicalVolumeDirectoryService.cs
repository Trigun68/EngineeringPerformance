using System.Diagnostics;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class PhysicalVolumeDirectoryService
    : DirectoryServiceBase<PhysicalVolume>, IPhysicalVolumeLookupService
{
    public PhysicalVolumeDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<PhysicalVolume> GetDbSet(AppDbContext context) => context.PhysicalVolumes;

    public override async Task<IReadOnlyList<PhysicalVolume>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        IQueryable<PhysicalVolume> query = GetDbSet(context).AsNoTracking();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .Select(e => new PhysicalVolume
            {
                Id = e.Id,
                ParentId = e.ParentId,
                SortOrder = e.SortOrder,
                IsArchived = e.IsArchived,
                Code = e.Code,
                Name = e.Name,
                Description = e.Description,
                VolumeTypeId = e.VolumeTypeId,
                ClassifierId = e.ClassifierId,
                UnitId = e.UnitId,
                IsAssessment = e.IsAssessment,
                Coefficient = e.Coefficient,
            })
            .ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] PhysicalVolume.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    public override async Task UpdateAsync(PhysicalVolume item)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();

        item.ModifiedAt = DateTime.UtcNow;

        // Один SQL UPDATE без предварительного SELECT и без change-tracking.
        // Перечисляем только редактируемые в форме поля, чтобы не затрагивать
        // навигации (в т.ч. ParentId/IsArchived управляются другими операциями).
        var affected = await context.PhysicalVolumes
            .Where(e => e.Id == item.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Code, item.Code)
                .SetProperty(e => e.Name, item.Name)
                .SetProperty(e => e.Description, item.Description)
                .SetProperty(e => e.VolumeTypeId, item.VolumeTypeId)
                .SetProperty(e => e.ClassifierId, item.ClassifierId)
                .SetProperty(e => e.ParentId, item.ParentId)
                .SetProperty(e => e.UnitId, item.UnitId)
                .SetProperty(e => e.IsAssessment, item.IsAssessment)
                .SetProperty(e => e.Coefficient, item.Coefficient)
                .SetProperty(e => e.DocumentSet, item.DocumentSet)
                .SetProperty(e => e.ModifiedAt, item.ModifiedAt));

        if (affected == 0)
            throw new InvalidOperationException($"PhysicalVolume with Id={item.Id} not found.");
    }

    public async Task<IReadOnlyList<PhysicalVolumeLookupItem>> GetLookupAsync(
        bool includeArchived = false,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.PhysicalVolumes.AsNoTracking();

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var all = await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .Select(e => new PhysicalVolumeLookupItem(
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
