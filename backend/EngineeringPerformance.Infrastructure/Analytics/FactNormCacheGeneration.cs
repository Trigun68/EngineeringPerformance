using System.Globalization;
using EngineeringPerformance.Application.Analytics.FactNorm;
using Microsoft.Extensions.Caching.Distributed;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Счётчик поколения кэша анализа «факт vs норма» в distributed cache (Redis / memory).
/// </summary>
public sealed class FactNormCacheGeneration(IDistributedCache cache) : IFactNormCacheGeneration
{
    private const string GenerationKey = "fact-norm:gen";
    private static readonly DistributedCacheEntryOptions GenerationEntryOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) };

    public async Task<long> GetAsync(CancellationToken cancellationToken = default)
    {
        var raw = await cache.GetStringAsync(GenerationKey, cancellationToken);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var generation)
            ? generation
            : 0;
    }

    public async Task BumpAsync(CancellationToken cancellationToken = default)
    {
        var generation = await GetAsync(cancellationToken);
        await cache.SetStringAsync(
            GenerationKey,
            (generation + 1).ToString(CultureInfo.InvariantCulture),
            GenerationEntryOptions,
            cancellationToken);
    }
}
