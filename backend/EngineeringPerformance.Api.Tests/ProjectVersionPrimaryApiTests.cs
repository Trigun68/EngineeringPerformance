using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class ProjectVersionPrimaryApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ProjectVersionPrimaryApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task PrimaryVersion_CreateSetPrimaryArchive_FollowsInvariants()
    {
        if (!_sqlAvailable) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "primary");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        async Task<VersionResponse> CreateAsync(string name, int direction, bool? isPrimary = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["versionName"] = name,
                ["name"] = name,
                ["direction"] = direction,
                ["physicalVolumeTypeId"] = 1,
            };
            if (isPrimary.HasValue)
                body["isPrimary"] = isPrimary.Value;

            var response = await _client.PostAsJsonAsync($"/api/projects/{project.Id}/versions", body);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<VersionResponse>())!;
        }

        var plan1 = await CreateAsync($"plan-1-{suffix}", direction: 1);
        plan1.IsPrimary.Should().BeTrue("first plan version is primary");

        var plan2 = await CreateAsync($"plan-2-{suffix}", direction: 1);
        plan2.IsPrimary.Should().BeFalse();

        var plan3 = await CreateAsync($"plan-3-{suffix}", direction: 1, isPrimary: true);
        plan3.IsPrimary.Should().BeTrue();

        var listAfterCreate = await _client.GetFromJsonAsync<VersionResponse[]>(
            $"/api/projects/{project.Id}/versions");
        listAfterCreate!.Where(v => v.Direction == 1 && v.IsPrimary).Should().ContainSingle()
            .Which.Id.Should().Be(plan3.Id);
        listAfterCreate.Single(v => v.Id == plan1.Id).IsPrimary.Should().BeFalse();

        var setPrimary = await _client.PostAsync(
            $"/api/projects/{project.Id}/versions/{plan2.Id}/set-primary",
            null);
        setPrimary.StatusCode.Should().Be(HttpStatusCode.OK);
        var plan2Primary = await setPrimary.Content.ReadFromJsonAsync<VersionResponse>();
        plan2Primary!.IsPrimary.Should().BeTrue();

        var listAfterSet = await _client.GetFromJsonAsync<VersionResponse[]>(
            $"/api/projects/{project.Id}/versions");
        listAfterSet!.Where(v => v.Direction == 1 && v.IsPrimary).Should().ContainSingle()
            .Which.Id.Should().Be(plan2.Id);

        // Update must not change IsPrimary
        var update = await _client.PutAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{plan2.Id}",
            new
            {
                versionName = plan2.VersionName,
                name = plan2.VersionName,
                direction = 1,
                physicalVolumeTypeId = 1,
                isPrimary = false,
            });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterUpdate = await update.Content.ReadFromJsonAsync<VersionResponse>();
        afterUpdate!.IsPrimary.Should().BeTrue();

        // Archive primary → promote next
        var archive = await _client.DeleteAsync($"/api/projects/{project.Id}/versions/{plan2.Id}");
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterArchive = await _client.GetFromJsonAsync<VersionResponse[]>(
            $"/api/projects/{project.Id}/versions");
        listAfterArchive!.Where(v => v.Direction == 1 && v.IsPrimary).Should().ContainSingle();

        var fact1 = await CreateAsync($"fact-1-{suffix}", direction: 2);
        fact1.IsPrimary.Should().BeTrue();
        listAfterArchive = await _client.GetFromJsonAsync<VersionResponse[]>(
            $"/api/projects/{project.Id}/versions");
        listAfterArchive!.Count(v => v.Direction == 1 && v.IsPrimary).Should().Be(1);
        listAfterArchive.Count(v => v.Direction == 2 && v.IsPrimary).Should().Be(1);
    }

    private sealed record VersionResponse(
        int Id,
        int ProjectId,
        string VersionName,
        int Direction,
        bool IsPrimary,
        bool IsArchived);
}
