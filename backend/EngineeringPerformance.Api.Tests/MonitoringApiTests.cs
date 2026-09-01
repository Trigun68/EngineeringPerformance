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
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
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
    public async Task ResourceMetrics_Admin_Returns200WithSeries()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/monitoring/resource-metrics?windowMinutes=15");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResourceMetricsResponse>();
        payload.Should().NotBeNull();
        payload!.Snapshot.Should().NotBeNull();
        payload.Snapshot.Backend.Available.Should().BeTrue();
        payload.Series.BackendCpuCores.Should().NotBeNull();
        payload.Verdicts.Should().NotBeEmpty();
        payload.WindowMinutes.Should().Be(15);
        payload.BucketSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ResourceMetrics_SevenDayWindow_Returns200()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/monitoring/resource-metrics?windowMinutes=10080");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ResourceMetricsResponse>();
        payload.Should().NotBeNull();
        payload!.WindowMinutes.Should().Be(10080);
        payload.BucketSeconds.Should().Be(600);
    }

    [Fact]
    public async Task SystemMetrics_Unauthenticated_Returns401()
    {
        var client = new ApiWebApplicationFactory().CreateClient();
        var response = await client.GetAsync("/api/monitoring/system-metrics");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResourceMetrics_Unauthenticated_Returns401()
    {
        var client = new ApiWebApplicationFactory().CreateClient();
        var response = await client.GetAsync("/api/monitoring/resource-metrics");
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

    private sealed record ResourceMetricsResponse(
        ResourceSnapshotResponse Snapshot,
        ResourceSeriesResponse Series,
        string[] Verdicts,
        int WindowMinutes,
        int BucketSeconds);

    private sealed record ResourceSnapshotResponse(ServiceResourceResponse Backend);

    private sealed record ServiceResourceResponse(bool Available);

    private sealed record ResourceSeriesResponse(object[] BackendCpuCores);
}
