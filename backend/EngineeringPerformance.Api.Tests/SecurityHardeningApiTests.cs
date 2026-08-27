using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class SecurityHardeningApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public SecurityHardeningApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task Refresh_WithRefreshCookieAndNoAccessToken_ReturnsOkAndPreservesCorrelationHeader()
    {
        if (!_sqlAvailable) return;

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "admin" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshCookie = ExtractCookie(loginResponse, "ep_refresh_token");

        const string correlationId = "refresh-security-test-id";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        request.Headers.TryAddWithoutValidation("Cookie", refreshCookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("X-Correlation-Id", out var headerValues).Should().BeTrue();
        headerValues.Should().NotBeNull();
        headerValues!.Single().Should().Be(correlationId);

        var payload = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        payload.Should().NotBeNull();
        payload!.User.Username.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SpnegoProfile_WhenAuthenticated_ReturnsDiagnosticPayload()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/auth/spnego-profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SpnegoProfileResponse>();
        payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Negotiate_WhenSpnegoDisabled_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/auth/negotiate");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record RefreshResponse(DateTime ExpiresAt, RefreshUserResponse User);
    private sealed record RefreshUserResponse(int Id, string Username);
    private sealed record SpnegoProfileResponse(
        bool SpnegoEnabled,
        bool TrustedProxyRequest,
        bool RemoteUserHeaderPresent,
        string? RemoteUser,
        bool DevWindowsUserConfigured,
        string? AuthMethod);

    private static string ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        var setCookie = response.Headers.GetValues("Set-Cookie")
            .Single(header => header.StartsWith($"{cookieName}=", StringComparison.Ordinal));
        return setCookie.Split(';', 2)[0];
    }
}
