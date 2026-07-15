using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ImportApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ImportApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task Import_InvalidDataType_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionId) = await TryGetFirstProjectVersionAsync();
        if (projectId is null || versionId is null) return;

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([]), "file", "empty.xlsx");

        var response = await _client.PostAsync(
            $"/api/projects/{projectId}/versions/{versionId}/import/unknown-type",
            content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Import_MissingFile_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionId) = await TryGetFirstProjectVersionAsync();
        if (projectId is null || versionId is null) return;

        using var content = new MultipartFormDataContent();
        var response = await _client.PostAsync(
            $"/api/projects/{projectId}/versions/{versionId}/import/equipment",
            content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListIterations_ReturnsOk()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionId) = await TryGetFirstProjectVersionAsync();
        if (projectId is null || versionId is null) return;

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/import/equipment/iterations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProjectsImportExcel_ReturnsNotImplemented()
    {
        if (!_sqlAvailable) return;

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x50, 0x4B]), "file", "projects.xlsx");

        var response = await _client.PostAsync("/api/projects/import/excel", content);
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
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
        if (versionsResponse.StatusCode != HttpStatusCode.OK) return (null, null);

        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsJson);
        if (versionsDoc.RootElement.GetArrayLength() == 0) return (null, null);

        var versionId = versionsDoc.RootElement[0].GetProperty("id").GetInt32();
        return (projectId, versionId);
    }
}
