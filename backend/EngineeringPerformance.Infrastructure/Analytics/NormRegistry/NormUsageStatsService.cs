using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EngineeringPerformance.Infrastructure.Analytics.NormRegistry;

public sealed class NormUsageStatsService(
    AppDbContext db,
    IDistributedCache cache,
    Microsoft.Extensions.Options.IOptions<NormHealthOptions> options) : INormUsageStatsService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<NormUsageStatsDto> GetUsageAsync(int normId, CancellationToken cancellationToken = default)
    {
        var stamp = await cache.GetStringAsync("norm-registry:invalidate-marker", cancellationToken) ?? "0";
        var cacheKey = $"norm-registry:usage:{stamp}:{normId}";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<NormUsageStatsDto>(cached, JsonOpts)!;

        var rows = await db.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => s.NormId == normId)
            .Select(s => new
            {
                s.ProjectAnalysisVersionId,
                s.ProjectAnalysisVersion.Direction,
                s.ProjectAnalysisVersion.IsPrimary,
                s.ProjectAnalysisVersion.CreatedAt,
                s.ProjectAnalysisVersion.IsArchived,
            })
            .Where(x => !x.IsArchived)
            .ToListAsync(cancellationToken);

        var distinctVersions = rows
            .GroupBy(r => r.ProjectAnalysisVersionId)
            .Select(g => g.First())
            .ToList();

        var plan = distinctVersions.Count(v => v.Direction == AnalysisDirection.Plan);
        var fact = distinctVersions.Count(v => v.Direction == AnalysisDirection.Fact);
        var primary = distinctVersions.Count(v => v.IsPrimary);
        DateTime? first = distinctVersions.Count > 0 ? distinctVersions.Min(v => v.CreatedAt) : null;
        DateTime? last = distinctVersions.Count > 0 ? distinctVersions.Max(v => v.CreatedAt) : null;

        var histogram = distinctVersions
            .GroupBy(v => v.CreatedAt.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyVersionCountDto(g.Key, g.Count()))
            .ToList();

        var dto = new NormUsageStatsDto(
            distinctVersions.Count,
            plan,
            fact,
            primary,
            first,
            last,
            histogram);

        var ttl = TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheTtlMinutes));
        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dto, JsonOpts),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyDictionary<int, NormUsageHint>> GetHintsAsync(
        IReadOnlyCollection<int>? normIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.AnalysisVersionSnapshots
            .AsNoTracking()
            .Where(s => !s.ProjectAnalysisVersion.IsArchived);

        if (normIds is { Count: > 0 })
            query = query.Where(s => normIds.Contains(s.NormId));

        var aggregated = await query
            .GroupBy(s => s.NormId)
            .Select(g => new
            {
                NormId = g.Key,
                Total = g.Select(x => x.ProjectAnalysisVersionId).Distinct().Count(),
                Last = g.Max(x => x.ProjectAnalysisVersion.CreatedAt),
            })
            .ToListAsync(cancellationToken);

        return aggregated.ToDictionary(
            x => x.NormId,
            x => new NormUsageHint(x.NormId, x.Total, x.Last));
    }
}
