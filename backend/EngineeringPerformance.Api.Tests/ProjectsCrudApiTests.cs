using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ProjectsCrudApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ProjectsCrudApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task CreateAndUpdateProject_WithEmptyWorkStartFields_Succeeds()
    {
        if (!_sqlAvailable) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "crud-empty");
        var updateResponse = await _client.PutAsJsonAsync($"/api/projects/{project.Id}", new
        {
            code = project.Code,
            name = $"{project.Name} updated",
            workStart = (string?)null,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ArchiveUnarchiveProject_TogglesIsArchived()
    {
        if (!_sqlAvailable) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "archive");

        var archiveResponse = await _client.PostAsync($"/api/projects/{project.Id}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        archived!.IsArchived.Should().BeTrue();

        var listHidden = await _client.GetFromJsonAsync<ProjectResponse[]>("/api/projects");
        listHidden!.Any(p => p.Id == project.Id).Should().BeFalse();

        var listWithArchived = await _client.GetFromJsonAsync<ProjectResponse[]>(
            "/api/projects?includeArchived=true");
        listWithArchived!.Any(p => p.Id == project.Id).Should().BeTrue();

        var unarchiveResponse = await _client.PostAsync($"/api/projects/{project.Id}/unarchive", null);
        unarchiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProjectsList_VersionCounts_ArePlanSlashFact_ExcludingArchived()
    {
        if (!_sqlAvailable) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "version-count");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        async Task<int> CreateVersionAsync(string name, int direction)
        {
            var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/versions", new
            {
                versionName = name,
                name,
                direction,
                physicalVolumeTypeId = 1,
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var version = await response.Content.ReadFromJsonAsync<VersionResponse>();
            return version!.Id;
        }

        await CreateVersionAsync($"plan-a-{suffix}", direction: 1);
        await CreateVersionAsync($"plan-b-{suffix}", direction: 1);
        var archivePlanId = await CreateVersionAsync($"plan-arch-{suffix}", direction: 1);
        await CreateVersionAsync($"fact-a-{suffix}", direction: 2);

        var archiveResponse = await _client.DeleteAsync(
            $"/api/projects/{project.Id}/versions/{archivePlanId}");
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<ProjectResponse[]>("/api/projects");
        var row = list!.Single(p => p.Id == project.Id);
        row.PlanVersionCount.Should().Be(2);
        row.FactVersionCount.Should().Be(1);
    }

    private sealed record ProjectResponse(
        int Id,
        string Code,
        string Name,
        bool IsArchived,
        int PlanVersionCount = 0,
        int FactVersionCount = 0);

    private sealed record VersionResponse(int Id, int ProjectId, string VersionName, bool IsArchived);
}
