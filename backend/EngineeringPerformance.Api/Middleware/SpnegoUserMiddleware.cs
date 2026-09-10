using System.Security.Claims;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Application.Auth;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Api.Middleware;

/// <summary>
/// Читает X-Remote-User только от доверенного прокси (проверка X-Trusted-Proxy-Token).
/// По умолчанию отключено (Spnego:Enabled=false) — защита от спуфинга при прямом доступе к API.
/// </summary>
public sealed class SpnegoUserMiddleware
{
    public const string RemoteUserHeader = "X-Remote-User";

    private readonly RequestDelegate _next;
    private readonly SpnegoOptions _options;

    public SpnegoUserMiddleware(RequestDelegate next, IOptions<SpnegoOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITrustedProxyValidator trustedProxy,
        ISpnegoIdentityResolver identityResolver)
    {
        if (_options.Enabled
            && context.User.Identity?.IsAuthenticated != true
            && IsTrustedProxyRequest(context, trustedProxy)
            && context.Request.Headers.TryGetValue(RemoteUserHeader, out var remoteUserValues))
        {
            var remoteUser = remoteUserValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(remoteUser))
            {
                var user = await identityResolver.ResolveAsync(remoteUser, context.RequestAborted);
                if (user is not null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new(ClaimTypes.GivenName, user.DisplayName),
                        new(ClaimTypes.Role, user.RoleName),
                        new("role_id", user.RoleId.ToString()),
                        new("auth_method", "spnego")
                    };

                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Spnego"));
                }
            }
        }

        await _next(context);
    }

    private static bool IsTrustedProxyRequest(HttpContext context, ITrustedProxyValidator trustedProxy)
    {
        context.Request.Headers.TryGetValue(SpnegoOptions.TrustedProxyTokenHeader, out var tokenValues);
        return trustedProxy.IsTrustedProxyToken(tokenValues.FirstOrDefault());
    }
}
