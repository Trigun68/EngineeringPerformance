namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "EngineeringPerformance";

    public string Audience { get; set; } = "EngineeringPerformance.Web";

    public int ExpirationMinutes { get; set; } = 60;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 7;

    public string AccessTokenCookieName { get; set; } = "ep_auth_token";

    public string RefreshTokenCookieName { get; set; } = "ep_refresh_token";

    public int ClockSkewSeconds { get; set; } = 30;

    public int EffectiveAccessTokenLifetimeMinutes =>
        Math.Clamp(AccessTokenLifetimeMinutes > 0 ? AccessTokenLifetimeMinutes : 15, 5, 60);
}
