using System.Diagnostics;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class LaborIntensityDirectoryService : DirectoryServiceBase<LaborIntensity>
{
    public LaborIntensityDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<LaborIntensity> GetDbSet(AppDbContext context) => context.LaborIntensities;

    public override async Task<IReadOnlyList<LaborIntensity>> GetAllAsync(bool includeArchived = false)
    {
        var swTotal = Stopwatch.StartNew();
        var swCtx = Stopwatch.StartNew();
        await using var context = await ContextFactory.CreateDbContextAsync();
        swCtx.Stop();

        var query = context.LaborIntensities
            .AsNoTracking()
            .Include(e => e.IicWorkDirection);

        IQueryable<LaborIntensity> filtered = includeArchived
            ? query
            : query.Where(e => !e.IsArchived);

        var swQuery = Stopwatch.StartNew();
        var list = await filtered
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Code)
            .ThenBy(e => e.Name)
            .ToListAsync();
        swQuery.Stop();
        swTotal.Stop();

        Log.Information(
            "[PERF] LaborIntensity.GetAllAsync archived={Arch} ctx={Ctx}ms query={Query}ms total={Total}ms rows={Rows}",
            includeArchived, swCtx.ElapsedMilliseconds, swQuery.ElapsedMilliseconds,
            swTotal.ElapsedMilliseconds, list.Count);

        return list;
    }
}
