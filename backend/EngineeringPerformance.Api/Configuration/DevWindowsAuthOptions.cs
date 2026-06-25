namespace EngineeringPerformance.Api.Configuration;

/// <summary>
/// Локальная имитация Windows auto-login (только Development).
/// На Linux Docker сервер не видит WindowsIdentity — задаётся вручную для dev-стенда.
/// </summary>
public sealed class DevWindowsAuthOptions
{
    public const string SectionName = "DevWindowsAuth";

    /// <summary>DOMAIN\user или логин приложения — как в WPF TryAutoLoginAsync.</summary>
    public string? SimulatedWindowsUser { get; set; }
}
