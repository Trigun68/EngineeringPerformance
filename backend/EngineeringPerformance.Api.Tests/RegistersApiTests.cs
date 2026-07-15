using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class RegistersApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public RegistersApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task CreateOverlappingNorm_ReturnsBadRequest()
    {
        if (!_sqlAvailable) return;

        var listResponse = await _client.GetAsync("/api/registers/iic-work-direction-norms");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetArrayLength() == 0)
            return;

        var existing = doc.RootElement[0];
        var workDirectionId = existing.GetProperty("workDirectionId").GetInt32();
        var effectiveFrom = existing.GetProperty("effectiveFrom").GetString()!;

        var duplicateResponse = await _client.PostAsJsonAsync("/api/registers/iic-work-direction-norms", new
        {
            workDirectionId,
            performance = 1.0,
            estimate = 1.0,
            standardDeviation = 0.1,
            effectiveFrom,
            effectiveTo = (string?)null,
        });

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListNorms_IncludesResolvedUnitName()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/registers/iic-work-direction-norms");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetArrayLength() == 0)
            return;

        var first = doc.RootElement[0];
        first.TryGetProperty("unitName", out _).Should().BeTrue();
        first.TryGetProperty("resolvedUnitId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateSecondPeriod_AutoClosesPreviousOpenRecord()
    {
        if (!_sqlAvailable) return;

        var directionsResponse = await _client.GetAsync("/api/dictionaries/all");
        directionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var directionsDoc = JsonDocument.Parse(await directionsResponse.Content.ReadAsStringAsync());
        var directions = directionsDoc.RootElement.GetProperty("iicWorkDirections");
        if (directions.GetArrayLength() == 0)
            return;

        var workDirectionId = directions[0].GetProperty("id").GetInt32();
        var dayOffset = Math.Abs(Guid.NewGuid().GetHashCode()) % 20 + 1;
        var firstFrom = $"2020-01-{dayOffset:D2}";
        var secondFrom = $"2021-06-{dayOffset:D2}";

        var createFirst = await _client.PostAsJsonAsync("/api/registers/iic-work-direction-norms", new
        {
            workDirectionId,
            performance = 10.0,
            estimate = 8.0,
            standardDeviation = 1.0,
            effectiveFrom = firstFrom,
            effectiveTo = (string?)null,
        });
        if (createFirst.StatusCode != HttpStatusCode.Created)
            return;

        var firstBody = await createFirst.Content.ReadFromJsonAsync<JsonElement>();
        var firstId = firstBody.GetProperty("id").GetInt32();

        var createSecond = await _client.PostAsJsonAsync("/api/registers/iic-work-direction-norms", new
        {
            workDirectionId,
            performance = 12.0,
            estimate = 9.0,
            standardDeviation = 1.1,
            effectiveFrom = secondFrom,
            effectiveTo = (string?)null,
        });
        createSecond.StatusCode.Should().Be(HttpStatusCode.Created);

        var historyResponse = await _client.GetAsync(
            $"/api/registers/iic-work-direction-norms?workDirectionId={workDirectionId}");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var historyDoc = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());

        JsonElement? closedFirst = null;
        foreach (var row in historyDoc.RootElement.EnumerateArray())
        {
            if (row.GetProperty("id").GetInt32() == firstId)
            {
                closedFirst = row;
                break;
            }
        }

        closedFirst.Should().NotBeNull();
        closedFirst!.Value.GetProperty("effectiveTo").ValueKind.Should().NotBe(JsonValueKind.Null);

        await _client.DeleteAsync($"/api/registers/iic-work-direction-norms/{firstId}");
        var secondId = (await createSecond.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await _client.DeleteAsync($"/api/registers/iic-work-direction-norms/{secondId}");
    }
}
