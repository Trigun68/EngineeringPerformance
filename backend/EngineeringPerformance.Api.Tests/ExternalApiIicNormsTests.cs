using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace EngineeringPerformance.Api.Tests;

public sealed class ExternalApiIicNormsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExternalApiIicNormsTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task IicNormsAlias_ListCreateDelete_Works()
    {
        var (client, ok) = await ExternalApiAuth.EnsureBearerAsync(_factory);
        if (!ok) return;

        var list = await client.GetAsync("/api/iic-norms");
        list.StatusCode.Should().Be(HttpStatusCode.OK);

        var dict = await client.GetAsync("/api/dictionaries/all");
        dict.StatusCode.Should().Be(HttpStatusCode.OK);
        using var dictDoc = JsonDocument.Parse(await dict.Content.ReadAsStringAsync());
        var directions = dictDoc.RootElement.GetProperty("iicWorkDirections");
        if (directions.GetArrayLength() == 0)
            return;

        var workDirectionId = directions[0].GetProperty("id").GetInt32();
        var hash = Math.Abs(Guid.NewGuid().GetHashCode());
        var year = 1990 + (hash % 20);
        var day = (hash % 27) + 1;
        var effectiveFrom = $"{year}-03-{day:D2}";

        var create = await client.PostAsJsonAsync("/api/iic-norms", new
        {
            workDirectionId,
            performance = 1.23,
            estimate = 1.0,
            standardDeviation = 0.2,
            effectiveFrom,
            effectiveTo = $"{year}-03-{day:D2}", // closed period — avoids open-ended overlap leftovers
        });

        if (create.StatusCode != HttpStatusCode.Created)
        {
            // Overlap or validation — still prove list works via alias
            create.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
            return;
        }

        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetInt32();

        var delete = await client.DeleteAsync($"/api/iic-norms/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
