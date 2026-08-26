using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiDictionariesTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiDictionariesTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task DictionariesAll_WithBearer_ReturnsKnownCollections()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var response = await client.GetAsync("/api/dictionaries/all");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        foreach (var key in new[]
                 {
                     "physicalVolumesKfo",
                     "physicalVolumesBer",
                     "equipments",
                     "laborIntensities",
                     "iicWorkDirections",
                     "unitsOfMeasurement",
                 })
        {
            root.TryGetProperty(key, out var arr).Should().BeTrue($"expected key {key}");
            arr.ValueKind.Should().Be(JsonValueKind.Array);
        }
    }
}
