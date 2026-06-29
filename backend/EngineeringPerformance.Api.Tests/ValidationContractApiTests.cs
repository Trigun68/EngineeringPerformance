using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ValidationContractApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ValidationContractApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsValidationProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "",
            password = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        root.GetProperty("title").GetString().Should().Be("Validation failed.");
        root.GetProperty("errorCode").GetString().Should().Be("validation_failed");
        root.GetProperty("errors").TryGetProperty("username", out _).Should().BeTrue();
        root.GetProperty("errors").TryGetProperty("password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ProjectVersionCreate_WithInvalidPayload_ReturnsValidationProblemDetails()
    {
        var sqlAvailable = await IntegrationTestAuth.TryLoginAsAdminAsync(_client);
        if (!sqlAvailable) return;

        var response = await _client.PostAsJsonAsync("/api/projects/1/versions", new
        {
            versionName = "",
            description = new string('x', 1200),
            direction = 999,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var errors = root.GetProperty("errors");

        root.GetProperty("title").GetString().Should().Be("Validation failed.");
        root.GetProperty("errorCode").GetString().Should().Be("validation_failed");
        errors.TryGetProperty("versionName", out _).Should().BeTrue();
        errors.TryGetProperty("direction", out _).Should().BeTrue();
    }
}
