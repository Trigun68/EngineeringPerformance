using System.Diagnostics;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectCharacteristicService : DirectoryServiceBase<ProjectCharacteristic>
{
    public ProjectCharacteristicService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<ProjectCharacteristic> GetDbSet(AppDbContext context) => context.ProjectCharacteristics;

    public override async Task<IReadOnlyList<ProjectCharacteristic>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        IQueryable<ProjectCharacteristic> query = context.ProjectCharacteristics
            .AsNoTracking()
            .Include(c => c.Options.OrderBy(o => o.SortOrder));

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await query
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Name)
            .ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] ProjectCharacteristic.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }

    public override async Task<ProjectCharacteristic?> GetByIdAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.ProjectCharacteristics
            .AsNoTracking()
            .Include(c => c.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>Создаёт характеристику вместе с её вариантами в одной транзакции.</summary>
    public async Task<ProjectCharacteristic> CreateWithOptionsAsync(
        ProjectCharacteristic item,
        IReadOnlyList<ProjectCharacteristicOption> options)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        item.CreatedAt = DateTime.UtcNow;
        item.Options = [];
        context.ProjectCharacteristics.Add(item);
        await context.SaveChangesAsync();

        var sortOrder = 0;
        foreach (var opt in options)
        {
            opt.Id = 0;
            opt.CharacteristicId = item.Id;
            opt.SortOrder = sortOrder++;
            context.ProjectCharacteristicOptions.Add(opt);
        }
        await context.SaveChangesAsync();
        return item;
    }

    /// <summary>Обновляет характеристику и полностью замещает её варианты (delete-and-insert).</summary>
    public async Task UpdateWithOptionsAsync(
        ProjectCharacteristic item,
        IReadOnlyList<ProjectCharacteristicOption> options)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var tracked = await context.ProjectCharacteristics
            .Include(c => c.Options)
            .FirstOrDefaultAsync(c => c.Id == item.Id)
            ?? throw new InvalidOperationException($"ProjectCharacteristic Id={item.Id} not found.");

        item.ModifiedAt = DateTime.UtcNow;
        context.Entry(tracked).CurrentValues.SetValues(item);

        context.ProjectCharacteristicOptions.RemoveRange(tracked.Options);

        var sortOrder = 0;
        foreach (var opt in options)
        {
            context.ProjectCharacteristicOptions.Add(new ProjectCharacteristicOption
            {
                CharacteristicId = item.Id,
                Name = opt.Name,
                ValuePercent = opt.ValuePercent,
                SortOrder = sortOrder++,
            });
        }

        await context.SaveChangesAsync();
    }
}
