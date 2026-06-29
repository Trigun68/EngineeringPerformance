using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class MonitoringApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public MonitoringApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.TryLoginAsAdminAsync(_client).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SystemMetrics_Admin_Returns200WithRedFields()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/monitoring/system-metrics?windowMinutes=15");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<SystemMetricsResponse>();
        payload.Should().NotBeNull();
        payload!.Red.TotalRequests.Should().BeGreaterThanOrEqualTo(0);
        payload.Database.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task SystemMetrics_Unauthenticated_Returns401()
    {
        var client = new ApiWebApplicationFactory().CreateClient();
        var response = await client.GetAsync("/api/monitoring/system-metrics");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PermissionDrift_Admin_Returns200()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/monitoring/permission-drift");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<PermissionDriftResponse>();
        payload.Should().NotBeNull();
        payload!.CheckedAtUtc.Should().NotBe(default);
    }

    private sealed record SystemMetricsResponse(
        RedMetricsResponse Red,
        DependencyHealthResponse Database);

    private sealed record RedMetricsResponse(int TotalRequests, double AverageDurationMs, int ServerErrorCount);

    private sealed record DependencyHealthResponse(bool IsConfigured, bool IsConnected, string Status);

    private sealed record PermissionDriftResponse(bool HasDrift, string[] UnknownObjects, DateTime CheckedAtUtc);
}
