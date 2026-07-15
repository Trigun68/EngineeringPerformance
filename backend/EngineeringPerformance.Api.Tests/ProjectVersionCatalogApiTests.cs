using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ProjectVersionCatalogApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ProjectVersionCatalogApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task CreateVersion_WithoutCatalogType_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var projectId = await GetFirstProjectIdAsync();
        if (projectId is null) return;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/versions",
            new
            {
                versionName = $"no-catalog-{suffix}",
                description = "catalog guard test",
                direction = 1,
                physicalVolumeTypeId = (int?)null,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<int?> GetFirstProjectIdAsync()
    {
        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return null;
        return projectsDoc.RootElement[0].GetProperty("id").GetInt32();
    }
}
