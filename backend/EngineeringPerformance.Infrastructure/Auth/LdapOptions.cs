namespace EngineeringPerformance.Infrastructure.Auth;

public sealed class LdapOptions
{
    public const string SectionName = "Ldap";

    public bool Enabled { get; set; }

    public string Server { get; set; } = string.Empty;

    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; }

    public string Domain { get; set; } = string.Empty;

    public string BaseDn { get; set; } = string.Empty;

    public string? BindUsername { get; set; }

    public string? BindPassword { get; set; }

    public string? GroupSearchBaseDn { get; set; }
}
