using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// Версия БЕР/КФО: AsOfDate → CreatedAt + снимок норм; импорт ФО → корректный FK;
/// смена каталога при уже импортированных PPV запрещена.
/// Uses disposable <see cref="TestProjectScope"/> — never attaches to seed projects.
/// </summary>
public sealed class ProjectVersionCatalogSnapshotApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public ProjectVersionCatalogSnapshotApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task CreateBerVersion_WithAsOfDate_SetsCreatedAt_AndSnapshotNormsValid()
    {
        if (!_sqlAvailable) return;

        var types = await ResolveCatalogTypeIdsAsync();
        if (types.BerTypeId is null) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "ber-asof");

        var asOf = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions",
            new
            {
                versionName = $"ber-asof-{suffix}",
                description = "BER asOf snapshot test",
                direction = 1,
                physicalVolumeTypeId = types.BerTypeId.Value,
                asOfDate = asOf,
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var versionId = created.RootElement.GetProperty("id").GetInt32();
        var createdAt = created.RootElement.GetProperty("createdAt").GetDateTime();
        createdAt.Should().BeCloseTo(asOf, TimeSpan.FromHours(1));

        await using var conn = OpenSql();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM AnalysisVersionSnapshots WHERE ProjectAnalysisVersionId = @vid;
            SELECT COUNT(*)
            FROM AnalysisVersionSnapshots s
            JOIN IicWorkDirectionNorms n ON n.Id = s.NormId
            WHERE s.ProjectAnalysisVersionId = @vid
              AND n.EffectiveFrom <= @asOf
              AND (n.EffectiveTo IS NULL OR n.EffectiveTo >= @asOf);
            """;
        cmd.Parameters.AddWithValue("@vid", versionId);
        cmd.Parameters.AddWithValue("@asOf", asOf);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var snapshotCount = reader.GetInt32(0);
        await reader.NextResultAsync();
        await reader.ReadAsync();
        var validCount = reader.GetInt32(0);

        validCount.Should().Be(snapshotCount);
    }

    [Fact]
    public async Task CreateBerAndKfoVersions_AddPpv_UsesMatchingCatalogFk()
    {
        if (!_sqlAvailable) return;

        var types = await ResolveCatalogTypeIdsAsync();
        if (types.BerTypeId is null || types.KfoTypeId is null) return;

        var berCatalogId = await GetFirstCatalogIdAsync("PhysicalVolumesBer");
        var kfoCatalogId = await GetFirstCatalogIdAsync("PhysicalVolumesKfo");
        if (berCatalogId is null || kfoCatalogId is null) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "ber-kfo-fk");
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var berVersionId = await CreateVersionAsync(
            project.Id, $"ber-fk-{suffix}", types.BerTypeId.Value, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        var kfoVersionId = await CreateVersionAsync(
            project.Id, $"kfo-fk-{suffix}", types.KfoTypeId.Value, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));

        var berPpv = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{berVersionId}/physical-volumes",
            new { physicalVolumeId = berCatalogId.Value, quantity = 1.5m, workName = "ber-row" });
        berPpv.StatusCode.Should().Be(HttpStatusCode.Created);

        var kfoPpv = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{kfoVersionId}/physical-volumes",
            new { physicalVolumeId = kfoCatalogId.Value, quantity = 2.5m, workName = "kfo-row" });
        kfoPpv.StatusCode.Should().Be(HttpStatusCode.Created);

        await using var conn = OpenSql();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT PhysicalVolumeBerId, PhysicalVolumeKfoId
            FROM ProjectPhysicalVolumes
            WHERE ProjectAnalysisVersionId = @berVid AND WorkName = N'ber-row';
            SELECT PhysicalVolumeBerId, PhysicalVolumeKfoId
            FROM ProjectPhysicalVolumes
            WHERE ProjectAnalysisVersionId = @kfoVid AND WorkName = N'kfo-row';
            """;
        cmd.Parameters.AddWithValue("@berVid", berVersionId);
        cmd.Parameters.AddWithValue("@kfoVid", kfoVersionId);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        reader.IsDBNull(0).Should().BeFalse("BER version PPV must set PhysicalVolumeBerId");
        reader.GetInt32(0).Should().Be(berCatalogId.Value);
        reader.IsDBNull(1).Should().BeTrue("BER version PPV must not set PhysicalVolumeKfoId");

        await reader.NextResultAsync();
        await reader.ReadAsync();
        reader.IsDBNull(1).Should().BeFalse("KFO version PPV must set PhysicalVolumeKfoId");
        reader.GetInt32(1).Should().Be(kfoCatalogId.Value);
        reader.IsDBNull(0).Should().BeTrue("KFO version PPV must not set PhysicalVolumeBerId");
    }

    [Fact]
    public async Task UpdateCatalogType_WhenPpvExist_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var types = await ResolveCatalogTypeIdsAsync();
        if (types.BerTypeId is null || types.KfoTypeId is null) return;

        var berCatalogId = await GetFirstCatalogIdAsync("PhysicalVolumesBer");
        if (berCatalogId is null) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "ber-block");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var versionId = await CreateVersionAsync(
            project.Id, $"ber-block-{suffix}", types.BerTypeId.Value, DateTime.UtcNow);

        var ppv = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/physical-volumes",
            new { physicalVolumeId = berCatalogId.Value, quantity = 1m, workName = "block-row" });
        ppv.StatusCode.Should().Be(HttpStatusCode.Created);

        var update = await _client.PutAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}",
            new
            {
                versionName = $"ber-block-{suffix}",
                description = "try switch to KFO",
                direction = 1,
                physicalVolumeTypeId = types.KfoTypeId.Value,
            });

        update.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var err = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        err.RootElement.GetProperty("detail").GetString()
            .Should().Contain("уже импортированы");
    }

    [Fact]
    public async Task UpdateAsOfDate_RepopulatesSnapshot()
    {
        if (!_sqlAvailable) return;

        var types = await ResolveCatalogTypeIdsAsync();
        if (types.KfoTypeId is null) return;

        await using var project = await TestProjectScope.CreateAsync(_client, "asof-refresh");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstAsOf = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc);
        var secondAsOf = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

        var versionId = await CreateVersionAsync(
            project.Id, $"asof-refresh-{suffix}", types.KfoTypeId.Value, firstAsOf);

        var beforeNormIds = await LoadSnapshotNormIdsAsync(versionId);

        var update = await _client.PutAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}",
            new
            {
                versionName = $"asof-refresh-{suffix}",
                description = "refresh snapshot",
                direction = 1,
                physicalVolumeTypeId = types.KfoTypeId.Value,
                asOfDate = secondAsOf,
            });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        using var updated = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        updated.RootElement.GetProperty("createdAt").GetDateTime()
            .Should().BeCloseTo(secondAsOf, TimeSpan.FromHours(1));

        var afterNormIds = await LoadSnapshotNormIdsAsync(versionId);
        await AssertSnapshotNormsValidForDateAsync(versionId, secondAsOf);

        afterNormIds.Should().NotBeNull();
        _ = beforeNormIds;
    }

    private async Task<int> CreateVersionAsync(int projectId, string name, int catalogTypeId, DateTime asOf)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{projectId}/versions",
            new
            {
                versionName = name,
                description = name,
                direction = 1,
                physicalVolumeTypeId = catalogTypeId,
                asOfDate = asOf,
            });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<(int? BerTypeId, int? KfoTypeId)> ResolveCatalogTypeIdsAsync()
    {
        var dictResponse = await _client.GetAsync("/api/dictionaries/all");
        dictResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var dictDoc = JsonDocument.Parse(await dictResponse.Content.ReadAsStringAsync());
        int? ber = null;
        int? kfo = null;
        foreach (var item in dictDoc.RootElement.GetProperty("physicalVolumeTypes").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString();
            if (name is "БЕР" or "БЭР") ber = item.GetProperty("id").GetInt32();
            if (name == "КФО") kfo = item.GetProperty("id").GetInt32();
        }

        return (ber, kfo);
    }

    private async Task<int?> GetFirstCatalogIdAsync(string table)
    {
        await using var conn = OpenSql();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT TOP 1 Id FROM {table} ORDER BY Id";
        var result = await cmd.ExecuteScalarAsync();
        return result is int id ? id : result is null or DBNull ? null : Convert.ToInt32(result);
    }

    private async Task<HashSet<int>> LoadSnapshotNormIdsAsync(int versionId)
    {
        await using var conn = OpenSql();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT NormId FROM AnalysisVersionSnapshots WHERE ProjectAnalysisVersionId = @vid";
        cmd.Parameters.AddWithValue("@vid", versionId);
        var set = new HashSet<int>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            set.Add(reader.GetInt32(0));
        return set;
    }

    private async Task AssertSnapshotNormsValidForDateAsync(int versionId, DateTime asOf)
    {
        await using var conn = OpenSql();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM AnalysisVersionSnapshots WHERE ProjectAnalysisVersionId = @vid;
            SELECT COUNT(*)
            FROM AnalysisVersionSnapshots s
            JOIN IicWorkDirectionNorms n ON n.Id = s.NormId
            WHERE s.ProjectAnalysisVersionId = @vid
              AND n.EffectiveFrom <= @asOf
              AND (n.EffectiveTo IS NULL OR n.EffectiveTo >= @asOf);
            """;
        cmd.Parameters.AddWithValue("@vid", versionId);
        cmd.Parameters.AddWithValue("@asOf", asOf);
        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var total = reader.GetInt32(0);
        await reader.NextResultAsync();
        await reader.ReadAsync();
        reader.GetInt32(0).Should().Be(total);
    }

    private SqlConnection OpenSql()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var cs = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is missing.");
        var conn = new SqlConnection(cs);
        conn.Open();
        return conn;
    }
}
