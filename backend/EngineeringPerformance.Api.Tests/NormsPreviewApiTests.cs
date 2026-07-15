using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class NormsPreviewApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public NormsPreviewApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task PreviewLinearNorm_ReturnsOk()
    {
        if (!_sqlAvailable) return;

        var response = await _client.PostAsJsonAsync("/api/norms/preview", new
        {
            normFormula = new
            {
                type = 1,
                coefficients = new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 0m },
                minVolume = 0m,
                maxVolume = 999999m,
                extrapolation = 2,
                piecewisePoints = (object?)null,
                polynomialCoeffs = (object?)null,
            },
            deviationFormula = new
            {
                modelType = 1,
                coefficients = new Dictionary<string, decimal> { ["sigma"] = 0.1m },
            },
            points = 100,
        });

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
    }
}
