using System.Security.Claims;
using EngineeringPerformance.Api.Configuration;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (_options.Enabled
            && context.User.Identity?.IsAuthenticated != true
            && IsTrustedProxyRequest(context)
            && context.Request.Headers.TryGetValue(RemoteUserHeader, out var remoteUserValues))
        {
            var remoteUser = remoteUserValues.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(remoteUser))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.IsActive &&
                        (u.WindowsUsername == remoteUser || u.Username == remoteUser));

                if (user is not null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new(ClaimTypes.GivenName, user.DisplayName),
                        new(ClaimTypes.Role, user.Role.Name),
                        new("role_id", user.RoleId.ToString()),
                        new("auth_method", "spnego")
                    };

                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Spnego"));
                }
            }
        }

        await _next(context);
    }

    private bool IsTrustedProxyRequest(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_options.TrustedProxySecret))
            return false;

        if (!context.Request.Headers.TryGetValue(SpnegoOptions.TrustedProxyTokenHeader, out var tokenValues))
            return false;

        var provided = tokenValues.FirstOrDefault();
        return !string.IsNullOrEmpty(provided)
            && string.Equals(provided, _options.TrustedProxySecret, StringComparison.Ordinal);
    }
}
