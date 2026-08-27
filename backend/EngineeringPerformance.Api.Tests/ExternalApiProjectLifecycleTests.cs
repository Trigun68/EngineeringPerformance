using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiProjectLifecycleTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiProjectLifecycleTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ProjectLifecycle_Characteristics_Version_Planning_Data_Succeeds()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        await using var project = await TestProjectScope.CreateAsync(client, "ext-life");

        var get = await client.GetAsync($"/api/projects/{project.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var put = await client.PutAsJsonAsync($"/api/projects/{project.Id}", new
        {
            code = project.Code,
            name = $"{project.Name} updated",
            workStart = (string?)null,
        });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var charsGet = await client.GetAsync($"/api/projects/{project.Id}/characteristics");
        charsGet.StatusCode.Should().Be(HttpStatusCode.OK);
        using var charsDoc = JsonDocument.Parse(await charsGet.Content.ReadAsStringAsync());
        if (charsDoc.RootElement.GetArrayLength() > 0)
        {
            var first = charsDoc.RootElement[0];
            var characteristicId = first.GetProperty("characteristicId").GetInt32();
            if (first.TryGetProperty("options", out var options) && options.GetArrayLength() > 0)
            {
                var optionId = options[0].GetProperty("id").GetInt32();
                var saveChars = await client.PutAsJsonAsync($"/api/projects/{project.Id}/characteristics", new
                {
                    selections = new[] { new { characteristicId, optionId } },
                });
                saveChars.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }
        }

        var kfoTypeId = await ResolveKfoTypeIdAsync(client);
        if (kfoTypeId is null) return;

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var createVersion = await client.PostAsJsonAsync($"/api/projects/{project.Id}/versions", new
        {
            versionName = $"ext-v-{suffix}",
            name = $"ext-v-{suffix}",
            direction = 1,
            physicalVolumeTypeId = kfoTypeId.Value,
        });
        createVersion.StatusCode.Should().Be(HttpStatusCode.Created);
        using var versionDoc = JsonDocument.Parse(await createVersion.Content.ReadAsStringAsync());
        var versionId = versionDoc.RootElement.GetProperty("id").GetInt32();

        var versionGet = await client.GetAsync($"/api/projects/{project.Id}/versions/{versionId}");
        versionGet.StatusCode.Should().Be(HttpStatusCode.OK);

        var planningGet = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/planning-parameters");
        planningGet.StatusCode.Should().Be(HttpStatusCode.OK);
        using var planningDoc = JsonDocument.Parse(await planningGet.Content.ReadAsStringAsync());
        if (planningDoc.RootElement.GetArrayLength() > 0)
        {
            var items = planningDoc.RootElement.EnumerateArray()
                .Select(row => new
                {
                    iicWorkDirectionId = row.GetProperty("iicWorkDirectionId").GetInt32(),
                    zPessimistic = row.GetProperty("zPessimistic").GetDecimal(),
                    zBase = row.GetProperty("zBase").GetDecimal(),
                    zOptimistic = row.GetProperty("zOptimistic").GetDecimal(),
                })
                .Take(5)
                .ToArray();

            var planningPut = await client.PutAsJsonAsync(
                $"/api/projects/{project.Id}/versions/{versionId}/planning-parameters",
                new { items });
            planningPut.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var kfoId = await GetFirstCatalogIdAsync("PhysicalVolumesKfo");
        if (kfoId is not null)
        {
            var ppv = await client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/versions/{versionId}/physical-volumes",
                new { physicalVolumeId = kfoId.Value, quantity = 1m, workName = "ext-ppv" });
            ppv.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var equipmentId = await GetFirstCatalogIdAsync("Equipments");
        if (equipmentId is not null)
        {
            var eq = await client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/versions/{versionId}/equipment",
                new { equipmentId = equipmentId.Value, quantity = 1m, quantityInWork = 0m });
            eq.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var liId = await GetFirstCatalogIdAsync("LaborIntensities");
        if (liId is not null)
        {
            var li = await client.PostAsJsonAsync(
                $"/api/projects/{project.Id}/versions/{versionId}/labor-intensities",
                new { laborIntensityId = liId.Value, plannedHours = 10m, actualHours = 0m });
            li.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var ppvList = await client.GetAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/physical-volumes?page=1&pageSize=10");
        ppvList.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private static async Task<int?> GetFirstCatalogIdAsync(string table)
    {
        var cs = Environment.GetEnvironmentVariable("EP_TEST_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? ApiWebApplicationFactory.ResolveDefaultConnectionString();
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT TOP 1 Id FROM {table} ORDER BY Id";
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt32(result);
    }
}
