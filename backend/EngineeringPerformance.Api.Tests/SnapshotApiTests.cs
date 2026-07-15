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
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
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

        var dictResponse = await _client.GetAsync("/api/dictionaries/all");
        dictResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dictJson = await dictResponse.Content.ReadAsStringAsync();
        using var dictDoc = System.Text.Json.JsonDocument.Parse(dictJson);
        int? kfoTypeId = null;
        foreach (var item in dictDoc.RootElement.GetProperty("physicalVolumeTypes").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == "КФО")
            {
                kfoTypeId = item.GetProperty("id").GetInt32();
                break;
            }
        }

        kfoTypeId.Should().NotBeNull();

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/versions",
            new
            {
                versionName = $"snapshot-test-{suffix}",
                description = "snapshot api test",
                direction = 1,
                physicalVolumeTypeId = kfoTypeId!.Value,
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
