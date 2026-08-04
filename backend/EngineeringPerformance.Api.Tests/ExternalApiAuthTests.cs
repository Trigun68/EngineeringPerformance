using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiAuthTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiAuthTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ReturnsAccessToken_AndBearerMe_Succeeds()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        using var bare = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        var login = await bare.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "admin" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.TryGetProperty("accessToken", out var tokenEl).Should().BeTrue();
        var token = tokenEl.GetString();
        token.Should().NotBeNullOrWhiteSpace();
        root.GetProperty("tokenType").GetString().Should().Be("Bearer");

        bare.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var me = await bare.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);

        using var noAuth = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        var unauthorized = await noAuth.GetAsync("/api/auth/me");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Cookie SPA flow still works
        using var cookieClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        var cookieLogin = await cookieClient.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "admin" });
        cookieLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookieMe = await cookieClient.GetAsync("/api/auth/me");
        cookieMe.StatusCode.Should().Be(HttpStatusCode.OK);

        _ = client;
    }
}
