using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiAnalyticsSmokeTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiAnalyticsSmokeTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PerformanceAndPageData_ReturnOk()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var kfoTypeId = await ResolveKfoTypeIdAsync(client);
        if (kfoTypeId is null) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-an");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createVersion = await client.PostAsJsonAsync($"/api/projects/{project.Id}/versions", new
        {
            versionName = $"an-{suffix}",
            direction = 1,
            physicalVolumeTypeId = kfoTypeId.Value,
        });
        createVersion.StatusCode.Should().Be(HttpStatusCode.Created);
        using var versionDoc = JsonDocument.Parse(await createVersion.Content.ReadAsStringAsync());
        var versionId = versionDoc.RootElement.GetProperty("id").GetInt32();

        var performance = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/performance");
        performance.StatusCode.Should().Be(HttpStatusCode.OK);

        var pageData = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/page-data");
        pageData.StatusCode.Should().Be(HttpStatusCode.OK);
        using var pageDoc = JsonDocument.Parse(await pageData.Content.ReadAsStringAsync());
        pageDoc.RootElement.GetProperty("forecast").GetProperty("detail").GetProperty("rows").GetArrayLength()
            .Should().Be(0);

        var pageDetails = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/page-data/detail");
        pageDetails.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<int?> ResolveKfoTypeIdAsync(HttpClient client)
    {
        var dict = await client.GetAsync("/api/dictionaries/all");
        if (!dict.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await dict.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("physicalVolumeTypes").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == "КФО")
                return item.GetProperty("id").GetInt32();
        }

        return null;
    }
}
