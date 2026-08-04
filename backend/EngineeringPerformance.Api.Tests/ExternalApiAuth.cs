using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// Auth helper for external-integration tests: JWT from login body, not cookie jar.
/// </summary>
internal static class ExternalApiAuth
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public sealed record LoginBody(
        DateTime ExpiresAt,
        string? AccessToken,
        string? TokenType,
        bool RequiresMfa = false);

    /// <summary>
    /// Creates a client without cookie handler auth pollution, logs in, sets Bearer header.
    /// Returns false if SQL/login unavailable.
    /// </summary>
    public static async Task<(HttpClient Client, bool Ok)> CreateBearerClientAsync(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "admin", password = "admin" },
                cts.Token);

            if (response.StatusCode != HttpStatusCode.OK)
                return (client, false);

            var body = await response.Content.ReadFromJsonAsync<LoginBody>(JsonOptions, cts.Token);
            if (body?.AccessToken is null || string.IsNullOrWhiteSpace(body.AccessToken))
                return (client, false);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(body.TokenType ?? "Bearer", body.AccessToken);
            return (client, true);
        }
        catch
        {
            return (client, false);
        }
    }

    public static async Task<(HttpClient Client, bool Ok)> EnsureBearerAsync(ApiWebApplicationFactory factory)
    {
        var (client, ok) = await CreateBearerClientAsync(factory);
        IntegrationTestSqlGate.RequireAvailable(ok);
        return (client, ok);
    }
}
