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
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
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
        using var compareDoc = System.Text.Json.JsonDocument.Parse(compareJson);
        compareDoc.RootElement.GetProperty("detail").TryGetProperty("columns", out _).Should().BeTrue();
        compareDoc.RootElement.GetProperty("detail").GetProperty("rows").GetArrayLength().Should().Be(0);

        var detailResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/compare/detail?versionA={versionA}&versionB={versionB}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var detailDoc = System.Text.Json.JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        detailDoc.RootElement.TryGetProperty("columns", out _).Should().BeTrue();
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
    public async Task CompareChart_ReturnsBoundedPoints_WhenTwoVersionsExist()
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

        var chartResponse = await _client.GetAsync(
            $"/api/projects/{projectId}/compare/chart?versionA={versionA}&versionB={versionB}&level=iic&metric=zScore&top=20");
        chartResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var chartJson = await chartResponse.Content.ReadAsStringAsync();
        chartJson.Should().Contain("points");
        using var chartDoc = System.Text.Json.JsonDocument.Parse(chartJson);
        var points = chartDoc.RootElement.GetProperty("points");
        points.GetArrayLength().Should().BeLessThanOrEqualTo(20);
    }

    [Fact]
    public async Task Trends_ReturnsSeries_WhenProjectHasVersions()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();

        var formulasResponse = await _client.GetAsync("/api/analytics/trends/formulas");
        formulasResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var trendsResponse = await _client.GetAsync(
            $"/api/analytics/trends?projectId={projectId}&formulaId=v1-docset-proportional&granularity=version");
        trendsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var trendsJson = await trendsResponse.Content.ReadAsStringAsync();
        trendsJson.Should().Contain("factLaborHours");
    }

    [Fact]
    public async Task CompareExportPdf_ReturnsPdfMagic_WhenTwoVersionsExist()
    {
        if (!_sqlAvailable) return;

        var (projectId, versionA, versionB) = await TryGetTwoVersionsAsync();
        if (projectId is null || versionA is null || versionB is null) return;

        var response = await _client.GetAsync(
            $"/api/projects/{projectId}/compare/export.pdf?versionA={versionA}&versionB={versionB}&section=summary");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes[0].Should().Be(0x25);
    }

    [Fact]
    public async Task TrendsExportPdf_ReturnsPdfMagic_WhenProjectExists()
    {
        if (!_sqlAvailable) return;

        var projectsResponse = await _client.GetAsync("/api/projects");
        if (projectsResponse.StatusCode != HttpStatusCode.OK) return;
        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return;
        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync(
            $"/api/analytics/trends/export.pdf?projectId={projectId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes[0].Should().Be(0x25);
    }

    [Fact]
    public async Task GroupAnalytics_ReturnsStructuredResult()
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
        var analysisJson = await analysisResponse.Content.ReadAsStringAsync();
        analysisJson.Should().Contain("projects");

        var trendResponse = await _client.GetAsync($"/api/projects/{projectId}/group-trend");
        trendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var trendJson = await trendResponse.Content.ReadAsStringAsync();
        trendJson.Should().Contain("trendsPagePath");
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

    private async Task<(int? ProjectId, int? VersionA, int? VersionB)> TryGetTwoVersionsAsync()
    {
        var projectsResponse = await _client.GetAsync("/api/projects");
        if (projectsResponse.StatusCode != HttpStatusCode.OK) return (null, null, null);

        var projectsJson = await projectsResponse.Content.ReadAsStringAsync();
        using var projectsDoc = System.Text.Json.JsonDocument.Parse(projectsJson);
        if (projectsDoc.RootElement.GetArrayLength() == 0) return (null, null, null);

        var projectId = projectsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionsResponse = await _client.GetAsync($"/api/projects/{projectId}/versions");
        if (versionsResponse.StatusCode != HttpStatusCode.OK) return (projectId, null, null);

        var versionsJson = await versionsResponse.Content.ReadAsStringAsync();
        using var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsJson);
        if (versionsDoc.RootElement.GetArrayLength() < 2) return (projectId, null, null);

        var versionA = versionsDoc.RootElement[0].GetProperty("id").GetInt32();
        var versionB = versionsDoc.RootElement[1].GetProperty("id").GetInt32();
        return (projectId, versionA, versionB);
    }
}
