using EngineeringPerformance.Application.Dictionaries;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using StackExchange.Redis;

namespace EngineeringPerformance.Infrastructure.Dictionaries;

/// <summary>
/// Инвалидация кэша справочников через IDistributedCache с retry при ошибках Redis.
/// </summary>
public sealed class DictionaryCacheInvalidator : ICacheInvalidator
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DictionaryCacheInvalidator> _logger;
    private readonly Polly.Retry.AsyncRetryPolicy _retryPolicy;

    public DictionaryCacheInvalidator(IDistributedCache cache, ILogger<DictionaryCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
        _retryPolicy = Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(100),
                onRetry: (exception, timeSpan, retryCount, ctx) =>
                {
                    _logger.LogWarning(exception,
                        "Redis cache invalidation failed. Retry {RetryCount}", retryCount);
                });
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
                await _cache.RemoveAsync(CachedDictionaryService.CacheKey, cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after retries");
            // Не бросаем исключение - инвалидация не критична
        }
    }
}
