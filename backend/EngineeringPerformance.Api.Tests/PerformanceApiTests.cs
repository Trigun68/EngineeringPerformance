using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class PerformanceApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public PerformanceApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task PhysicalVolumes_LargePage_ReturnsOk_WithinReasonableTime()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionId) = await TryGetFirstProjectVersionAsync();
        if (projectId is null || versionId is null) return;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/physical-volumes?page=1&pageSize=1000");
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeLessThan(30_000, "PPV pageSize=1000 should respond under 30s");
    }

    private async Task<(int? ProjectId, int? VersionId)> TryGetFirstProjectVersionAsync()
    {
        var projectsResponse = await _client.GetAsync("/api/projects");
        if (projectsResponse.StatusCode != HttpStatusCode.OK) return (null, null);

        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return (null, null);

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionsResponse = await _client.GetAsync($"/api/projects/{projectId}/versions");
        if (versionsResponse.StatusCode != HttpStatusCode.OK) return (projectId, null);

        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsJson);
        if (versionsDoc.RootElement.GetArrayLength() == 0) return (projectId, null);

        var versionId = versionsDoc.RootElement[0].GetProperty("id").GetInt32();
        return (projectId, versionId);
    }
}
