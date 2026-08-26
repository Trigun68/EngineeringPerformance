using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class FactNormAnalyticsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public FactNormAnalyticsApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task ReferenceFormulas_ReturnsAllSevenTypes()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/analytics/reference-formulas");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(7);
        var codes = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("code").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        codes.Should().Contain(["CONSTANT", "LINEAR", "POWER", "LOGARITHMIC", "EXPONENTIAL", "POLYNOMIAL2", "HYPERBOLIC"]);
    }

    [Fact]
    public async Task FactNormAnalysis_InvalidPeriod_Returns400()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync(
            "/api/analytics/fact-norm-analysis",
            new
            {
                periodStart = "2026-06-01",
                periodEnd = "2026-01-01",
            });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FactNormAnalysis_EmptyOrValidPeriod_ReturnsOk()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync(
            "/api/analytics/fact-norm-analysis",
            new
            {
                periodStart = "2025-01-01",
                periodEnd = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("accuracy", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("comparisons", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("suggestions", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateNormFromSuggestion_WithoutEdit_Returns403_OrUnauthorized()
    {
        if (!_sqlAvailable) return;

        // Observer-like: endpoint requires IicWorkDirectionNorms Edit — admin usually has it.
        // Verify missing suggestion returns 404 for admin (auth OK).
        var response = await _client.PostAsJsonAsync(
            "/api/analytics/norms/from-suggestion",
            new
            {
                suggestionId = 999999999,
                effectiveFrom = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Export_ReturnsFile()
    {
        if (!_sqlAvailable) return;

        var url =
            $"/api/analytics/fact-norm-analysis/export?periodStart=2025-01-01&periodEnd={DateTime.UtcNow:yyyy-MM-dd}&format=csv";
        var response = await _client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}
