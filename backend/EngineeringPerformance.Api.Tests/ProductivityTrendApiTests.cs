using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ProductivityTrendApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ProductivityTrendApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task ProductivityTrend_ReturnsShape()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync("/api/analytics/trends/productivity", new
        {
            periodStart = (string?)null,
            periodEnd = (string?)null,
            volumeMode = "median",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        root.GetProperty("volumeMode").GetString().Should().Be("median");
        root.TryGetProperty("overall", out _).Should().BeTrue();
        root.TryGetProperty("byGroup", out _).Should().BeTrue();
        root.TryGetProperty("legend", out _).Should().BeTrue();
        root.TryGetProperty("transitions", out _).Should().BeTrue();
        root.TryGetProperty("counterfactuals", out _).Should().BeTrue();
        root.TryGetProperty("warnings", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ProductivityTrend_InvalidVolumeMode_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync("/api/analytics/trends/productivity", new
        {
            volumeMode = "mean",
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
