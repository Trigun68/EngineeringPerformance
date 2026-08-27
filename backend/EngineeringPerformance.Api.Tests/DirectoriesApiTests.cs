using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class DirectoriesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly object Sync = new();
    private static HttpClient? _sharedClient;
    private static bool _sqlAvailable;

    private readonly HttpClient _client;

    public DirectoriesApiTests(ApiWebApplicationFactory factory)
    {
        lock (Sync)
        {
            if (_sharedClient is null)
            {
                _sharedClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
                _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_sharedClient);
            }

            _client = _sharedClient;
        }
    }

    [Fact]
    public async Task UnitsOfMeasurement_CreateUpdateArchive_WhenAuthenticatedAsAdmin()
    {
        if (!_sqlAvailable) return;

        var uniqueName = $"test-unit-{Guid.NewGuid():N}"[..24];
        var createResponse = await _client.PostAsJsonAsync("/api/directories/units-of-measurement", new
        {
            name = uniqueName,
            sortOrder = 0,
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var updateResponse = await _client.PutAsJsonAsync($"/api/directories/units-of-measurement/{id}", new
        {
            name = $"{uniqueName}-upd",
            sortOrder = 1,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var archiveResponse = await _client.PostAsync($"/api/directories/units-of-measurement/{id}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PhysicalVolumes_TreeAndChildren_ReturnData()
    {
        if (!_sqlAvailable) return;

        var treeResponse = await _client.GetAsync("/api/directories/physical-volumes-kfo/tree");
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tree = await treeResponse.Content.ReadFromJsonAsync<JsonElement>();
        tree.ValueKind.Should().Be(JsonValueKind.Array);

        if (tree.GetArrayLength() > 0)
        {
            var props = tree[0].GetProperty("properties");
            props.TryGetProperty("code", out _).Should().BeTrue("properties should use camelCase keys");
            props.TryGetProperty("Code", out _).Should().BeFalse("properties must not use PascalCase keys");
        }

        var childrenResponse = await _client.GetAsync("/api/directories/physical-volumes-kfo/children?parentId=1");
        childrenResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PhysicalVolumesKfo_InvalidClassifierId_ReturnsNotFound()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync("/api/directories/physical-volumes-kfo", new
        {
            name = $"fk-test-{Guid.NewGuid():N}"[..20],
            sortOrder = 0,
            properties = new { classifierId = 9_999_999 },
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Регрессия: смена coefficient на каталоге, связанном с проектными данными,
    /// раньше давала JsonException (цикл nav ↔ DirectoryItem) при ToDto после recalc.
    /// </summary>
    [Theory]
    [InlineData("physical-volumes-kfo")]
    [InlineData("physical-volumes-ber")]
    [InlineData("equipment")]
    public async Task AssessmentCatalog_UpdateCoefficient_WhenUsedInProject_ReturnsOk(string resource)
    {
        if (!_sqlAvailable) return;

        var treeResponse = await _client.GetAsync($"/api/directories/{resource}/tree");
        if (treeResponse.StatusCode == HttpStatusCode.NotFound)
        {
            // equipment может требовать projectId — fallback на list
            var listResponse = await _client.GetAsync($"/api/directories/{resource}");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var items = list.ValueKind == JsonValueKind.Array
                ? list
                : list.TryGetProperty("items", out var arr) ? arr : default;
            items.ValueKind.Should().Be(JsonValueKind.Array);
            items.GetArrayLength().Should().BeGreaterThan(0);
            await UpdateCoefficientRoundTrip(resource, items[0]);
            return;
        }

        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tree = await treeResponse.Content.ReadFromJsonAsync<JsonElement>();
        tree.ValueKind.Should().Be(JsonValueKind.Array);
        if (tree.GetArrayLength() == 0)
        {
            // CI seed может не содержать деревья BER/equipment — fallback на list.
            var listResponse = await _client.GetAsync($"/api/directories/{resource}");
            listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
            var items = list.ValueKind == JsonValueKind.Array
                ? list
                : list.TryGetProperty("items", out var arr) ? arr : default;
            if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return;
            await UpdateCoefficientRoundTrip(resource, items[0]);
            return;
        }

        var leaf = FindFirstLeafWithCoefficient(tree);
        leaf.Should().NotBeNull($"ожидается узел {resource} с properties.coefficient");
        await UpdateCoefficientRoundTrip(resource, leaf!.Value);
    }

    private async Task UpdateCoefficientRoundTrip(string resource, JsonElement node)
    {
        var id = node.GetProperty("id").GetInt32();
        var name = node.GetProperty("name").GetString()!;
        var parentId = node.TryGetProperty("parentId", out var pid) && pid.ValueKind != JsonValueKind.Null
            ? pid.GetInt32()
            : (int?)null;
        var sortOrder = node.TryGetProperty("sortOrder", out var so) ? so.GetInt32() : 0;
        var props = node.GetProperty("properties");
        var originalCoef = props.TryGetProperty("coefficient", out var c) ? c.GetDecimal() : 1m;
        var nextCoef = originalCoef == 1m ? 1.5m : 1m;

        var updateResponse = await _client.PutAsJsonAsync($"/api/directories/{resource}/{id}", new
        {
            name,
            parentId,
            sortOrder,
            properties = BuildPropertiesWithCoefficient(props, nextCoef),
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            because: await updateResponse.Content.ReadAsStringAsync());
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        updated.GetProperty("properties").GetProperty("coefficient").GetDecimal().Should().Be(nextCoef);

        var restoreResponse = await _client.PutAsJsonAsync($"/api/directories/{resource}/{id}", new
        {
            name,
            parentId,
            sortOrder,
            properties = BuildPropertiesWithCoefficient(props, originalCoef),
        });
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static JsonElement? FindFirstLeafWithCoefficient(JsonElement nodes)
    {
        foreach (var node in nodes.EnumerateArray())
        {
            if (node.TryGetProperty("properties", out var props)
                && props.TryGetProperty("coefficient", out _))
                return node;

            if (node.TryGetProperty("children", out var children)
                && children.ValueKind == JsonValueKind.Array
                && children.GetArrayLength() > 0)
            {
                var nested = FindFirstLeafWithCoefficient(children);
                if (nested is not null)
                    return nested;
            }
        }

        return null;
    }

    private static Dictionary<string, object?> BuildPropertiesWithCoefficient(JsonElement props, decimal coefficient)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in props.EnumerateObject())
        {
            map[p.Name] = p.Value.ValueKind switch
            {
                JsonValueKind.Number => p.Value.TryGetInt64(out var l) ? l : p.Value.GetDecimal(),
                JsonValueKind.String => p.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => p.Value.ToString(),
            };
        }

        map["coefficient"] = coefficient;
        return map;
    }

    [Theory]
    [InlineData("physical-volumes-kfo")]
    [InlineData("physical-volumes-ber")]
    [InlineData("equipment")]
    [InlineData("labor-intensities")]
    [InlineData("iic-work-directions")]
    [InlineData("units-of-measurement")]
    [InlineData("project-characteristics")]
    public async Task AllDirectoryResources_List_ReturnsOk(string resource)
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync($"/api/directories/{resource}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
