using EngineeringPerformance.Application.Auth;

namespace EngineeringPerformance.Api.Auth;

public sealed class AuthCookieHelper(IHostEnvironment environment)
{
    public void Append(HttpResponse response, string accessToken, DateTime accessExpires, RefreshTokenIssue refreshToken)
    {
        response.Cookies.Append(AuthCookieConstants.TokenCookieName, accessToken, CreateOptions(accessExpires, response));
        response.Cookies.Append(AuthCookieConstants.RefreshCookieName, refreshToken.Token, CreateOptions(refreshToken.ExpiresAt, response));
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(AuthCookieConstants.TokenCookieName, new CookieOptions { Path = "/" });
        response.Cookies.Delete(AuthCookieConstants.RefreshCookieName, new CookieOptions { Path = "/" });
    }

    private CookieOptions CreateOptions(DateTime expires, HttpResponse response) =>
        new()
        {
            HttpOnly = true,
            Secure = environment.IsProduction() || response.HttpContext.Request.IsHttps,
            // Strict ломает cookie jar HttpClient в интеграционных тестах (WebApplicationFactory).
            SameSite = environment.IsEnvironment("Development") || environment.IsEnvironment("Testing")
                ? SameSiteMode.Lax
                : SameSiteMode.Strict,
            Expires = expires,
            MaxAge = expires - DateTime.UtcNow,
            Path = "/",
        };
}
