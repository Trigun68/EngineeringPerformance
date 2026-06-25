using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class DirectoriesApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public DirectoriesApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.TryLoginAsAdminAsync(_client).GetAwaiter().GetResult();
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

        var treeResponse = await _client.GetAsync("/api/directories/physical-volumes/tree");
        treeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tree = await treeResponse.Content.ReadFromJsonAsync<JsonElement>();
        tree.ValueKind.Should().Be(JsonValueKind.Array);

        if (tree.GetArrayLength() > 0)
        {
            var props = tree[0].GetProperty("properties");
            props.TryGetProperty("code", out _).Should().BeTrue("properties should use camelCase keys");
            props.TryGetProperty("Code", out _).Should().BeFalse("properties must not use PascalCase keys");
        }

        var childrenResponse = await _client.GetAsync("/api/directories/physical-volumes/children?parentId=1");
        childrenResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("physical-volume-types")]
    [InlineData("physical-volumes")]
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
