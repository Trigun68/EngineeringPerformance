namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; } = true;

    public int LoginMaxAttempts { get; set; } = 5;

    public int LoginWindowMinutes { get; set; } = 15;

    public int RefreshMaxPerMinute { get; set; } = 10;

    public int NegotiateMaxPerMinute { get; set; } = 20;

    public int LockoutDurationMinutes { get; set; } = 15;
}
