using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class SnapshotApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public SnapshotApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.TryLoginAsAdminAsync(_client).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CreateVersion_ThenPerformance_ReturnsOk()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/versions",
            new
            {
                versionName = $"snapshot-test-{suffix}",
                description = "snapshot api test",
                direction = 0,
                physicalVolumeTypeId = (int?)null,
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdJson = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = System.Text.Json.JsonDocument.Parse(createdJson);
        var versionId = createdDoc.RootElement.GetProperty("id").GetInt32();

        var performanceResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/performance");
        performanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
