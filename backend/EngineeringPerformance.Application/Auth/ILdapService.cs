namespace EngineeringPerformance.Application.Auth;

public sealed record LdapAuthenticationResult(
    string Username,
    string? DisplayName,
    string? Email);

public interface ILdapService
{
    Task<LdapAuthenticationResult?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
