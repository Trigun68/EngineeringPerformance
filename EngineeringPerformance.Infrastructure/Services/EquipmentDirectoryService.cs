using System.Diagnostics;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class EquipmentDirectoryService : DirectoryServiceBase<Equipment>
{
    public EquipmentDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<Equipment> GetDbSet(AppDbContext context) => context.Equipments;

    public override async Task<IReadOnlyList<Equipment>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        IQueryable<Equipment> query = GetDbSet(context).AsNoTracking();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .Select(e => new Equipment
            {
                Id = e.Id,
                ParentId = e.ParentId,
                SortOrder = e.SortOrder,
                IsArchived = e.IsArchived,
                Name = e.Name,
                Description = e.Description,
                ClassifierId = e.ClassifierId,
                IsAssessment = e.IsAssessment,
                Coefficient = e.Coefficient,
                UnitId = e.UnitId,
            })
            .ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] Equipment.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    public override async Task<Equipment> CreateAsync(Equipment item)
    {
        item.CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(item.Name, item.UnitId);
        return await base.CreateAsync(item);
    }

    public override async Task UpdateAsync(Equipment item)
    {
        item.CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(item.Name, item.UnitId);
        await using var context = await ContextFactory.CreateDbContextAsync();

        item.ModifiedAt = DateTime.UtcNow;

        var affected = await context.Equipments
            .Where(e => e.Id == item.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Name, item.Name)
                .SetProperty(e => e.Description, item.Description)
                .SetProperty(e => e.ClassifierId, item.ClassifierId)
                .SetProperty(e => e.ParentId, item.ParentId)
                .SetProperty(e => e.IsAssessment, item.IsAssessment)
                .SetProperty(e => e.Coefficient, item.Coefficient)
                .SetProperty(e => e.UnitId, item.UnitId)
                .SetProperty(e => e.CatalogKeyHash, item.CatalogKeyHash)
                .SetProperty(e => e.ModifiedAt, item.ModifiedAt));

        if (affected == 0)
            throw new InvalidOperationException($"Equipment with Id={item.Id} not found.");
    }
}
