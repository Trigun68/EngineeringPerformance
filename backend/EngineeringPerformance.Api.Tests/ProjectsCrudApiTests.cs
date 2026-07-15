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

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            code = $"T-{suffix}",
            name = $"Test project {suffix}",
            workStart = (string?)null,
            workNumber = (string?)null,
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        created.Should().NotBeNull();

        var updateResponse = await _client.PutAsJsonAsync($"/api/projects/{created!.Id}", new
        {
            code = created.Code,
            name = $"{created.Name} updated",
            workStart = (string?)null,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await _client.DeleteAsync($"/api/projects/{created.Id}");
    }

    [Fact]
    public async Task ArchiveUnarchiveProject_TogglesIsArchived()
    {
        if (!_sqlAvailable) return;

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createResponse = await _client.PostAsJsonAsync("/api/projects", new
        {
            code = $"A-{suffix}",
            name = $"Archive test {suffix}",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        created.Should().NotBeNull();

        var archiveResponse = await _client.PostAsync($"/api/projects/{created!.Id}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archived = await archiveResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        archived!.IsArchived.Should().BeTrue();

        var listHidden = await _client.GetFromJsonAsync<ProjectResponse[]>("/api/projects");
        listHidden!.Any(p => p.Id == created.Id).Should().BeFalse();

        var listWithArchived = await _client.GetFromJsonAsync<ProjectResponse[]>(
            "/api/projects?includeArchived=true");
        listWithArchived!.Any(p => p.Id == created.Id).Should().BeTrue();

        var unarchiveResponse = await _client.PostAsync($"/api/projects/{created.Id}/unarchive", null);
        unarchiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await _client.DeleteAsync($"/api/projects/{created.Id}");
    }

    private sealed record ProjectResponse(int Id, string Code, string Name, bool IsArchived);
}
