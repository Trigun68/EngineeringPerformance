using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Settings;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Settings;

/// <summary>
/// Глобальные настройки системы в distributed cache (Redis / memory).
/// Значение по умолчанию — из эффективного срока access JWT в конфигурации.
/// </summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private const string SessionExpirationKey = "system:settings:sessionExpirationMinutes";

    private readonly IDistributedCache _cache;
    private readonly JwtSettings _jwtDefaults;
    private readonly IUserActionLogger _actionLogger;

    public SystemSettingsService(
        IDistributedCache cache,
        IOptions<JwtSettings> jwtDefaults,
        IUserActionLogger actionLogger)
    {
        _cache = cache;
        _jwtDefaults = jwtDefaults.Value;
        _actionLogger = actionLogger;
    }

    public async Task<int> GetSessionExpirationMinutesAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _cache.GetStringAsync(SessionExpirationKey, cancellationToken);
        if (int.TryParse(raw, out var minutes) && minutes is >= 5 and <= 10080)
            return minutes;

        return _jwtDefaults.EffectiveAccessTokenLifetimeMinutes;
    }

    public async Task SetSessionExpirationMinutesAsync(int minutes, CancellationToken cancellationToken = default)
    {
        if (minutes is < 5 or > 10080)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Session expiration must be between 5 and 10080 minutes.");

        await _cache.SetStringAsync(
            SessionExpirationKey,
            minutes.ToString(),
            cancellationToken);

        await _actionLogger.LogAsync(
            UserActionType.SystemSettingUpdate,
            entityType: "SystemSetting",
            description: $"Session expiration set to {minutes} minutes",
            cancellationToken: cancellationToken);
    }
}
