using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class DictionariesAclApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public DictionariesAclApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        using var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/dictionaries/all");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAdmin_ReturnsOk()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/dictionaries/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();

        var cached = new HttpRequestMessage(HttpMethod.Get, "/api/dictionaries/all");
        cached.Headers.TryAddWithoutValidation("If-None-Match", response.Headers.ETag!.ToString());
        var notModified = await _client.SendAsync(cached);
        notModified.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Fact]
    public async Task GetAll_WithoutReferencesPermission_ReturnsForbidden()
    {
        if (!_sqlAvailable) return;

        // Engineer в seed часто имеет References=Read — временно снимаем право через admin API.
        var rightsResponse = await _client.GetAsync("/api/access-rights");
        rightsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rightsJson = await rightsResponse.Content.ReadAsStringAsync();
        using var rightsDoc = JsonDocument.Parse(rightsJson);

        int? rightId = null;
        int previousLevel = 1;
        foreach (var item in rightsDoc.RootElement.EnumerateArray())
        {
            var roleName = item.GetProperty("roleName").GetString();
            var objectName = item.GetProperty("objectName").GetString();
            if (!string.Equals(roleName, "Engineer", StringComparison.Ordinal)
                || !string.Equals(objectName, "References", StringComparison.Ordinal))
            {
                continue;
            }

            rightId = item.GetProperty("id").GetInt32();
            previousLevel = item.GetProperty("accessLevel").GetInt32();
            break;
        }

        if (rightId is null)
            return;

        try
        {
            var revoke = await _client.PutAsJsonAsync(
                $"/api/access-rights/{rightId}",
                new { accessLevel = 0 });
            revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

            using var observerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var loginCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var login = await observerClient.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "observer", password = "observer" },
                loginCts.Token);
            if (login.StatusCode != HttpStatusCode.OK)
                return;

            var response = await observerClient.GetAsync("/api/dictionaries/all");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await _client.PutAsJsonAsync(
                $"/api/access-rights/{rightId}",
                new { accessLevel = previousLevel });
        }
    }
}
