namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "EngineeringPerformance";

    public string Audience { get; set; } = "EngineeringPerformance.Web";

    public int ExpirationMinutes { get; set; } = 60;
}
