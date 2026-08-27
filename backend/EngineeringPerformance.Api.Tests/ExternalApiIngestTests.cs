using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiIngestTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiIngestTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task IngestLabor_Replace_AndPreview_Succeeds()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ingest");
        var versionId = await CreateVersionAsync(client, project.Id);
        if (versionId is null) return;

        var preview = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/labor-intensities/preview",
            new { documentTypeNames = new[] { "ext-unmatched-doc-type" } });
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        using var previewDoc = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
        previewDoc.RootElement.GetProperty("totalDataRows").GetInt32().Should().Be(1);

        var ingest = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/labor-intensities",
            new
            {
                mode = "replace",
                description = "ext-ingest-li",
                items = new[]
                {
                    new { documentTypeName = "ext-unmatched-doc-type", plannedHours = 12m, actualHours = 0m, brand = "B1" },
                },
            });
        ingest.StatusCode.Should().Be(HttpStatusCode.OK);
        using var ingestDoc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        ingestDoc.RootElement.GetProperty("importedRows").GetInt32().Should().Be(1);

        var list = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/labor-intensities?page=1&pageSize=10");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IngestPhysicalVolumes_EmptyReplace_ReturnsBadRequest()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ingest-empty");
        var versionId = await CreateVersionAsync(client, project.Id);
        if (versionId is null) return;

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/physical-volumes",
            new { mode = "replace", items = Array.Empty<object>() });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IngestPhysicalVolumes_UnknownMode_ReturnsBadRequest()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ingest-mode");
        var versionId = await CreateVersionAsync(client, project.Id);
        if (versionId is null) return;

        var response = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/physical-volumes",
            new { mode = "merge", items = new[] { new { epCode = "x", quantity = 1m } } });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IngestPhysicalVolumes_WithCatalogCode_ImportsRow()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var catalog = await GetFirstKfoCodeAsync();
        if (catalog is null) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ingest-ppv");
        var versionId = await CreateVersionAsync(client, project.Id);
        if (versionId is null) return;

        var ingest = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/physical-volumes",
            new
            {
                mode = "replace",
                items = new[]
                {
                    new
                    {
                        epCode = catalog.Value.Code,
                        quantity = 3m,
                        facilityCode = "F-1",
                        facilityName = "Facility",
                        workId = "W-1",
                        workName = "ext-ppv",
                    },
                },
            });
        ingest.IsSuccessStatusCode.Should().BeTrue(
            because: await ingest.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("importedRows").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task IngestEquipment_ByCatalogName_Succeeds()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-ingest-eq");
        var versionId = await CreateVersionAsync(client, project.Id);
        if (versionId is null) return;

        var ingest = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/ingest/equipment",
            new
            {
                mode = "replace",
                items = new[]
                {
                    new { catalogName = $"ext-eq-{Guid.NewGuid():N}"[..24], unitName = "шт", quantity = 2m },
                },
            });
        ingest.IsSuccessStatusCode.Should().BeTrue(
            because: await ingest.Content.ReadAsStringAsync());
        using var doc = JsonDocument.Parse(await ingest.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("importedRows").GetInt32().Should().Be(1);

        var delete = await client.DeleteAsync($"/api/projects/{project.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<int?> CreateVersionAsync(HttpClient client, int projectId)
    {
        var kfoTypeId = await ResolveKfoTypeIdAsync(client);
        if (kfoTypeId is null) return null;

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createVersion = await client.PostAsJsonAsync($"/api/projects/{projectId}/versions", new
        {
            versionName = $"ext-ing-{suffix}",
            name = $"ext-ing-{suffix}",
            direction = 1,
            physicalVolumeTypeId = kfoTypeId.Value,
        });
        if (createVersion.StatusCode != HttpStatusCode.Created)
            return null;

        using var versionDoc = JsonDocument.Parse(await createVersion.Content.ReadAsStringAsync());
        return versionDoc.RootElement.GetProperty("id").GetInt32();
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

    private static async Task<(int Id, string Code)?> GetFirstKfoCodeAsync()
    {
        var cs = Environment.GetEnvironmentVariable("EP_TEST_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? ApiWebApplicationFactory.ResolveDefaultConnectionString();
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TOP 1 Id, Code FROM PhysicalVolumesKfo WHERE Code IS NOT NULL AND LEN(Code) > 0 ORDER BY Id";
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;
        return (reader.GetInt32(0), reader.GetString(1));
    }
}
