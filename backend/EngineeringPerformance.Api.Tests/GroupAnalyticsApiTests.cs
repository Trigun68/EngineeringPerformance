using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class GroupAnalyticsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public GroupAnalyticsApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task GroupAnalysis_ReturnsProjectRows_WithAggregateZ()
    {
        if (!_sqlAvailable) return;

        var projectId = await TryGetFirstProjectIdAsync();
        if (projectId is null) return;

        var response = await _client.GetAsync($"/api/projects/{projectId}/group-analysis");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("groupCriterion", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("projects", out var projects).Should().BeTrue();
        projects.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.TryGetProperty("aggregateWeightedZScore", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GroupTrend_ReturnsTrendsPagePath()
    {
        if (!_sqlAvailable) return;

        var projectId = await TryGetFirstProjectIdAsync();
        if (projectId is null) return;

        var response = await _client.GetAsync($"/api/projects/{projectId}/group-trend");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("trendsPagePath");
        json.Should().Contain($"/analytics/trends?projectId={projectId}");
    }

    private async Task<int?> TryGetFirstProjectIdAsync()
    {
        var projectsResponse = await _client.GetAsync("/api/projects");
        if (projectsResponse.StatusCode != HttpStatusCode.OK) return null;

        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return null;

        return projectsDoc.RootElement[0].GetProperty("id").GetInt32();
    }
}
