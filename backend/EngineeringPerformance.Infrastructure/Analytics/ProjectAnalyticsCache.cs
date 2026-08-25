using System.Globalization;
using System.Text.Json;
using EngineeringPerformance.Application.Analytics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Реализация кэша аналитики проекта над distributed cache (Redis в проде, memory в dev).
/// Инвалидация — инкремент поколения: перебирать ключи в Redis не нужно.
/// </summary>
public sealed class ProjectAnalyticsCache(IDistributedCache cache, ILogger<ProjectAnalyticsCache> logger)
    : IProjectAnalyticsCache
{
    private const string GlobalGenerationKey = "analytics:gen:all";
    // Свежесть даёт generation (invalidate после persist). Короткий TTL устраивал холодный stampede.
    private static readonly TimeSpan EntryLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan GenerationLifetime = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<T?> GetAsync<T>(int projectId, string key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var payload = await cache.GetStringAsync(
                await BuildKeyAsync(projectId, key, cancellationToken),
                cancellationToken);

            return string.IsNullOrEmpty(payload) ? null : JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Недоступный Redis не должен ломать чтение — просто считаем заново.
            logger.LogWarning(ex, "Analytics cache read failed for project {ProjectId} key {Key}", projectId, key);
            return null;
        }
    }

    public async Task SetAsync<T>(int projectId, string key, T value, CancellationToken cancellationToken = default)
        where T : class
    {
        // Расчёт уже готов: запись в Redis не должна зависеть от отмены HTTP.
        // Иначе уход клиента с вкладки превращает успешный compute в 500, и кэш пустой.
        _ = cancellationToken;
        try
        {
            await cache.SetStringAsync(
                await BuildKeyAsync(projectId, key, CancellationToken.None),
                JsonSerializer.Serialize(value, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = EntryLifetime },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Analytics cache write failed for project {ProjectId} key {Key}", projectId, key);
        }
    }

    public Task InvalidateProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
        BumpAsync(ProjectGenerationKey(projectId), cancellationToken);

    public Task InvalidateAllAsync(CancellationToken cancellationToken = default) =>
        BumpAsync(GlobalGenerationKey, cancellationToken);

    private async Task<string> BuildKeyAsync(int projectId, string key, CancellationToken cancellationToken)
    {
        var global = await ReadGenerationAsync(GlobalGenerationKey, cancellationToken);
        var project = await ReadGenerationAsync(ProjectGenerationKey(projectId), cancellationToken);
        return $"analytics:p{projectId}:g{global}.{project}:{key}";
    }

    private async Task BumpAsync(string generationKey, CancellationToken cancellationToken)
    {
        try
        {
            var next = await ReadGenerationAsync(generationKey, cancellationToken) + 1;
            await cache.SetStringAsync(
                generationKey,
                next.ToString(CultureInfo.InvariantCulture),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = GenerationLifetime },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Не смогли поднять поколение — записи истекут по TTL; мутацию не срываем.
            logger.LogWarning(ex, "Analytics cache invalidation failed for {GenerationKey}", generationKey);
        }
    }

    private async Task<long> ReadGenerationAsync(string generationKey, CancellationToken cancellationToken)
    {
        var raw = await cache.GetStringAsync(generationKey, cancellationToken);
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var generation)
            ? generation
            : 0;
    }

    private static string ProjectGenerationKey(int projectId) => $"analytics:gen:p{projectId}";
}
