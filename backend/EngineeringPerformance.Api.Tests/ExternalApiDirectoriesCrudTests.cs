using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiDirectoriesCrudTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiDirectoriesCrudTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("physical-volumes-kfo")]
    [InlineData("physical-volumes-ber")]
    [InlineData("equipment")]
    [InlineData("labor-intensities")]
    [InlineData("iic-work-directions")]
    [InlineData("units-of-measurement")]
    [InlineData("project-characteristics")]
    public async Task DirectorySlug_List_ReturnsOk(string slug)
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var response = await client.GetAsync($"/api/directories/{slug}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnitsOfMeasurement_CreateGetUpdateArchive_RoundTrip()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var uniqueName = $"ext-unit-{Guid.NewGuid():N}"[..24];
        var create = await client.PostAsJsonAsync("/api/directories/units-of-measurement", new
        {
            name = uniqueName,
            sortOrder = 0,
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdDoc = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("id").GetInt32();

        var get = await client.GetAsync($"/api/directories/units-of-measurement/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await client.PutAsJsonAsync($"/api/directories/units-of-measurement/{id}", new
        {
            name = $"{uniqueName}-upd",
            sortOrder = 1,
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var archive = await client.PostAsync($"/api/directories/units-of-measurement/{id}/archive", null);
        archive.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
