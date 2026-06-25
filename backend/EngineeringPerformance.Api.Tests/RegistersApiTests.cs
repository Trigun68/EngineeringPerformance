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
        _sqlAvailable = IntegrationTestAuth.TryLoginAsAdminAsync(_client).GetAwaiter().GetResult();
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
}
