using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace EngineeringPerformance.Infrastructure.Metrics;

public sealed class SystemMetricsService : ISystemMetricsService
{
    private readonly IRequestMetricsStore _metricsStore;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;

    public SystemMetricsService(
        IRequestMetricsStore metricsStore,
        AppDbContext context,
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _metricsStore = metricsStore;
        _context = context;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<SystemMetricsDto> GetSystemMetricsAsync(
        int windowMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        var clampedWindow = Math.Clamp(windowMinutes, 1, 60);
        var snapshot = _metricsStore.GetSnapshot(clampedWindow);

        var database = await CheckDatabaseAsync(cancellationToken);
        var redis = await CheckRedisAsync(cancellationToken);

        return new SystemMetricsDto(
            snapshot.Red,
            snapshot.RequestRateSeries,
            snapshot.ErrorDistribution,
            snapshot.ResponseTimeTrend24h,
            database,
            redis,
            DateTime.UtcNow);
    }

    private async Task<DependencyHealthDto> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return new DependencyHealthDto(
                IsConfigured: true,
                IsConnected: canConnect,
                Status: canConnect ? "Connected" : "Unavailable");
        }
        catch (Exception ex)
        {
            return new DependencyHealthDto(true, false, ex.Message);
        }
    }

    private async Task<DependencyHealthDto> CheckRedisAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DependencyHealthDto(
                IsConfigured: false,
                IsConnected: false,
                Status: "Not configured");
        }

        try
        {
            // Реальная проверка подключения к Redis через кэш
            var testKey = "health:systemmetrics:" + Guid.NewGuid().ToString("N");
            await _cache.SetStringAsync(testKey, "ok", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            }, cancellationToken);

            var value = await _cache.GetStringAsync(testKey, cancellationToken);
            await _cache.RemoveAsync(testKey, cancellationToken);

            if (value == "ok")
            {
                return new DependencyHealthDto(
                    IsConfigured: true,
                    IsConnected: true,
                    Status: "Connected");
            }

            return new DependencyHealthDto(
                IsConfigured: true,
                IsConnected: false,
                Status: "Unexpected response");
        }
        catch (Exception ex)
        {
            return new DependencyHealthDto(
                IsConfigured: true,
                IsConnected: false,
                Status: ex.Message);
        }
    }
}
