namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public bool SeedDefaultAdmin { get; set; } = true;

    public string DefaultAdminUsername { get; set; } = "admin";

    public string DefaultAdminPassword { get; set; } = string.Empty;
}
