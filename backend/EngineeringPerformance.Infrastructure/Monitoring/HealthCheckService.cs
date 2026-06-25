using System.Diagnostics;
using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class HealthCheckService : IHealthCheckService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IConfiguration _configuration;
    private readonly IRequestMetricsStore _metricsStore;
    private readonly IDistributedCache _cache;

    public HealthCheckService(
        IDbContextFactory<AppDbContext> contextFactory,
        IConfiguration configuration,
        IRequestMetricsStore metricsStore,
        IDistributedCache cache)
    {
        _contextFactory = contextFactory;
        _configuration = configuration;
        _metricsStore = metricsStore;
        _cache = cache;
    }

    public async Task<HealthCheckResultDto> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var services = new List<ServiceHealthDto>
        {
            await CheckDatabaseAsync(cancellationToken),
            await CheckRedisAsync(cancellationToken),
            await CheckExternalApiAsync(cancellationToken),
        };

        var trend = _metricsStore.GetSnapshot(15).ResponseTimeTrend24h;

        return new HealthCheckResultDto(DateTime.UtcNow, services, trend);
    }

    private async Task<ServiceHealthDto> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var ok = await context.Database.CanConnectAsync(cancellationToken);
            sw.Stop();
            return new ServiceHealthDto(
                "Database",
                true,
                ok,
                ok ? "Connected" : "Unavailable",
                Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceHealthDto("Database", true, false, ex.Message, Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
    }

    private async Task<ServiceHealthDto> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ServiceHealthDto("Redis", false, false, "Not configured", null);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            // Проверяем реальное подключение через IDistributedCache
            var testKey = "health:check:" + Guid.NewGuid().ToString("N");
            await _cache.SetStringAsync(testKey, "ok", new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) 
            }, cancellationToken);
            
            var value = await _cache.GetStringAsync(testKey, cancellationToken);
            await _cache.RemoveAsync(testKey, cancellationToken);
            
            sw.Stop();
            
            if (value == "ok")
            {
                return new ServiceHealthDto(
                    "Redis", 
                    true, 
                    true, 
                    "Connected", 
                    Math.Round(sw.Elapsed.TotalMilliseconds, 2));
            }
            
            return new ServiceHealthDto(
                "Redis", 
                true, 
                false, 
                "Unexpected response from cache", 
                Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceHealthDto(
                "Redis", 
                true, 
                false, 
                ex.Message, 
                Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
    }

    private async Task<ServiceHealthDto> CheckExternalApiAsync(CancellationToken cancellationToken)
    {
        var url = _configuration["Monitoring:ExternalHealthUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            return new ServiceHealthDto("External API", false, true, "Not configured (skipped)", null);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(url, cancellationToken);
            sw.Stop();
            var healthy = response.IsSuccessStatusCode;
            return new ServiceHealthDto(
                "External API",
                true,
                healthy,
                healthy ? $"HTTP {(int)response.StatusCode}" : $"HTTP {(int)response.StatusCode}",
                Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceHealthDto("External API", true, false, ex.Message, Math.Round(sw.Elapsed.TotalMilliseconds, 2));
        }
    }
}
