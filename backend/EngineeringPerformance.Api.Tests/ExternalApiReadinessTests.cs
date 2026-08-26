using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiReadinessTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiReadinessTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Readiness_EmptyDataBlocks_ReportsIssues_ThenClearsAfterPpv()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var kfoTypeId = await ResolveKfoTypeIdAsync(client);
        if (kfoTypeId is null) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ready");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createVersion = await client.PostAsJsonAsync($"/api/projects/{project.Id}/versions", new
        {
            versionName = $"ready-{suffix}",
            direction = 1,
            physicalVolumeTypeId = kfoTypeId.Value,
        });
        createVersion.StatusCode.Should().Be(HttpStatusCode.Created);
        using var versionDoc = JsonDocument.Parse(await createVersion.Content.ReadAsStringAsync());
        var versionId = versionDoc.RootElement.GetProperty("id").GetInt32();

        var readiness = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/readiness");
        readiness.StatusCode.Should().Be(HttpStatusCode.OK);
        using var readyDoc = JsonDocument.Parse(await readiness.Content.ReadAsStringAsync());
        var root = readyDoc.RootElement;
        root.GetProperty("hasPhysicalVolumeType").GetBoolean().Should().BeTrue();
        root.GetProperty("hasNormSnapshots").GetBoolean().Should().BeTrue();
        root.GetProperty("physicalVolumesCount").GetInt32().Should().Be(0);
        root.GetProperty("issues").GetArrayLength().Should().BeGreaterThan(0);

        var issueCodes = root.GetProperty("issues").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString())
            .ToHashSet(StringComparer.Ordinal);
        issueCodes.Should().Contain("missing_physical_volumes");

        var kfoId = await TryFirstKfoIdAsync(client);
        if (kfoId is null) return;

        var ppv = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/physical-volumes",
            new { physicalVolumeId = kfoId.Value, quantity = 1m, workName = "ready-ppv" });
        ppv.StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/readiness");
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterDoc = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        afterDoc.RootElement.GetProperty("physicalVolumesCount").GetInt32().Should().BeGreaterThan(0);
        var afterCodes = afterDoc.RootElement.GetProperty("issues").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString())
            .ToHashSet(StringComparer.Ordinal);
        afterCodes.Should().NotContain("missing_physical_volumes");
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

    private static async Task<int?> TryFirstKfoIdAsync(HttpClient client)
    {
        var list = await client.GetAsync("/api/directories/physical-volumes-kfo?page=1&pageSize=1");
        if (!list.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("items", out var arr) ? arr : default;
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
            return null;
        return items[0].GetProperty("id").GetInt32();
    }
}
