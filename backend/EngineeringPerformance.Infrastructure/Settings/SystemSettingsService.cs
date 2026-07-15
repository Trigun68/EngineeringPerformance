using EngineeringPerformance.Application.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using EngineeringPerformance.Infrastructure.Auth;

namespace EngineeringPerformance.Infrastructure.Settings;

/// <summary>
/// Глобальные настройки системы в distributed cache (Redis / memory).
/// Значение по умолчанию — из Jwt:ExpirationMinutes в конфигурации.
/// </summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private const string SessionExpirationKey = "system:settings:sessionExpirationMinutes";

    private readonly IDistributedCache _cache;
    private readonly JwtSettings _jwtDefaults;

    public SystemSettingsService(IDistributedCache cache, IOptions<JwtSettings> jwtDefaults)
    {
        _cache = cache;
        _jwtDefaults = jwtDefaults.Value;
    }

    public async Task<int> GetSessionExpirationMinutesAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _cache.GetStringAsync(SessionExpirationKey, cancellationToken);
        if (int.TryParse(raw, out var minutes) && minutes is >= 5 and <= 10080)
            return minutes;

        return _jwtDefaults.ExpirationMinutes;
    }

    public async Task SetSessionExpirationMinutesAsync(int minutes, CancellationToken cancellationToken = default)
    {
        if (minutes is < 5 or > 10080)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Session expiration must be between 5 and 10080 minutes.");

        await _cache.SetStringAsync(
            SessionExpirationKey,
            minutes.ToString(),
            cancellationToken);
    }
}
