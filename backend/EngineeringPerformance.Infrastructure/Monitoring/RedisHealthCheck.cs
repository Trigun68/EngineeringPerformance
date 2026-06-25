using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Monitoring;

/// <summary>
/// Health check для Redis через IDistributedCache.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IDistributedCache cache, ILogger<RedisHealthCheck> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем подключение к Redis через кэш
            await _cache.SetStringAsync("health:check", "ok", new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) 
            }, cancellationToken);
            
            var value = await _cache.GetStringAsync("health:check", cancellationToken);
            
            if (value == "ok")
                return HealthCheckResult.Healthy("Redis connection is healthy");
            
            return HealthCheckResult.Degraded("Redis returned unexpected value");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}
