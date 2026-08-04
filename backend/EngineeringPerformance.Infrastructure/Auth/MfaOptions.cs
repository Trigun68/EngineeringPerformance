namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class MfaOptions
{
    public const string SectionName = "Mfa";

    public bool Enabled { get; set; }

    public string Issuer { get; set; } = "EngineeringPerformance";

    public string[] RequiredRoles { get; set; } = ["SuperAdmin", "Admin"];

    public bool OptionalForOthers { get; set; } = true;

    public string SecretEncryptionPurpose { get; set; } = "EngineeringPerformance.MfaSecret.v1";

    public int TotpWindowSeconds { get; set; } = 30;

    public int TotpTolerance { get; set; } = 1;
}
