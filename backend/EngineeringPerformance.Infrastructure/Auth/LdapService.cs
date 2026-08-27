using EngineeringPerformance.Application.Auth;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Auth;

/// <summary>
/// Временная точка интеграции LDAP. Сетевые вызовы не выполняются, пока
/// корпоративный LDAP-провайдер не подключён.
/// </summary>
public sealed class LdapService(IOptions<LdapOptions> options) : ILdapService
{
    private readonly LdapOptions _options = options.Value;

    public Task<LdapAuthenticationResult?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<LdapAuthenticationResult?>(null);
        }

        return Task.FromResult<LdapAuthenticationResult?>(null);
    }
}
