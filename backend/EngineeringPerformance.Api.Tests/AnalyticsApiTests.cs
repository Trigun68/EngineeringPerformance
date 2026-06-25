using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class AnalyticsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public AnalyticsApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.TryLoginAsAdminAsync(_client).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Performance_ReturnsOk_WhenProjectAndVersionExist()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionsResponse = await _client.GetAsync($"/api/projects/{projectId}/versions");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsJson);
        if (versionsDoc.RootElement.GetArrayLength() == 0) return;

        var versionId = versionsDoc.RootElement[0].GetProperty("id").GetInt32();

        var performanceResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/performance");
        performanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var analysisResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/analysis");
        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var forecastResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/forecast");
        forecastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Compare_ReturnsOk_WhenTwoVersionsExist()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionsResponse = await _client.GetAsync($"/api/projects/{projectId}/versions");
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsJson);
        if (versionsDoc.RootElement.GetArrayLength() < 2) return;

        var versionA = versionsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionB = versionsDoc.RootElement[1].GetProperty("id").GetInt32();

        var compareResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/compare?versionA={versionA}&versionB={versionB}");
        compareResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var compareJson = await compareResponse.Content.ReadAsStringAsync();
        compareJson.Should().Contain("summary");
        compareJson.Should().Contain("facility");
        compareJson.Should().Contain("detail");
    }

    [Fact]
    public async Task Export_EndpointsReturnBinary_WhenVersionExists()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionId) = await TryGetFirstProjectVersionAsync();
        if (projectId is null || versionId is null) return;

        var excelResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/export/analysis.xlsx");
        excelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        excelResponse.Content.Headers.ContentType?.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var pdfResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/versions/{versionId}/export/analysis.pdf");
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task GroupAnalytics_ReturnsPlaceholder()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();

        var analysisResponse = await _client.GetAsync($"/api/projects/{projectId}/group-analysis");
        analysisResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var trendResponse = await _client.GetAsync($"/api/projects/{projectId}/group-trend");
        trendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Audit_ReturnsOk_ForAdministrator()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/audit/user-actions?take=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AccessRights_ReturnsOk_ForAdministrator()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/access-rights");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
