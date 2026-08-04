namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class AuthAuditOptions
{
    public const string SectionName = "AuthAudit";

    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 365;
}
