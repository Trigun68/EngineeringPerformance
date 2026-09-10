using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EngineeringPerformance.Api.Tests;

public sealed class WorkCostsApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public WorkCostsApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task List_WithoutAuth_ReturnsUnauthorized()
    {
        using var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/registers/work-costs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAsOf_ReturnsFullIicTree()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/registers/work-costs?asOf=2026-08-01");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        if (doc.RootElement.GetArrayLength() == 0)
            return;

        var first = doc.RootElement[0];
        first.TryGetProperty("workDirectionId", out _).Should().BeTrue();
        first.TryGetProperty("workDirectionName", out _).Should().BeTrue();
        first.TryGetProperty("ratePerHour", out _).Should().BeTrue();
        first.TryGetProperty("effectiveFrom", out _).Should().BeTrue();
    }

    [Fact]
    public async Task BatchOrganization_WritesRateToAllDirections()
    {
        if (!_sqlAvailable) return;

        var list = await _client.GetAsync("/api/registers/work-costs?asOf=2026-01-01");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var directionCount = listDoc.RootElement.GetArrayLength();
        if (directionCount == 0)
            return;

        // Уникальная далёкая дата: повторные прогоны не пересекаются с хвостами 2097-03-*.
        var n = Math.Abs(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
        var from = $"{2100 + n % 80:D4}-{(n % 12) + 1:D2}-{(n % 27) + 1:D2}";

        var batch = await _client.PostAsJsonAsync("/api/registers/work-costs/batch", new
        {
            effectiveFrom = from,
            level = 1,
            items = new[] { new { workDirectionId = 0, ratePerHour = 1234.5m } },
        });
        batch.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await batch.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("writtenCount").GetInt32().Should().Be(directionCount);

        var asOf = await _client.GetAsync($"/api/registers/work-costs?asOf={from}");
        asOf.StatusCode.Should().Be(HttpStatusCode.OK);
        using var asOfDoc = JsonDocument.Parse(await asOf.Content.ReadAsStringAsync());
        foreach (var row in asOfDoc.RootElement.EnumerateArray())
        {
            row.GetProperty("ratePerHour").GetDecimal().Should().Be(1234.5m);
        }
    }

    [Fact]
    public async Task ExportExcel_UsesCanonicalFileName()
    {
        if (!_sqlAvailable) return;

        var response = await _client.GetAsync("/api/registers/work-costs/export.xlsx");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var fileName = ResolveDownloadFileName(response);
        fileName.Should().Contain("стоимость_работ");
        fileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task Batch_AsObserver_ReturnsForbidden()
    {
        if (!_sqlAvailable) return;

        using var observer = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await observer.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "observer", password = "observer" });
        if (login.StatusCode != HttpStatusCode.OK)
            return;

        var response = await observer.PostAsJsonAsync("/api/registers/work-costs/batch", new
        {
            effectiveFrom = "2099-01-01",
            level = 1,
            items = new[] { new { workDirectionId = 0, ratePerHour = 1m } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Periods_ListEditDelete_RoundTrip()
    {
        if (!_sqlAvailable) return;

        var list = await _client.GetAsync("/api/registers/work-costs?asOf=2026-01-01");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        if (listDoc.RootElement.GetArrayLength() == 0)
            return;

        var from = $"2096-05-{(Math.Abs(Guid.NewGuid().GetHashCode()) % 27) + 1:D2}";

        var batch = await _client.PostAsJsonAsync("/api/registers/work-costs/batch", new
        {
            effectiveFrom = from,
            level = 1,
            items = new[] { new { workDirectionId = 0, ratePerHour = 77.7m } },
        });
        batch.StatusCode.Should().Be(HttpStatusCode.OK);

        var periods = await _client.GetAsync("/api/registers/work-costs/periods");
        periods.StatusCode.Should().Be(HttpStatusCode.OK);
        using var periodsDoc = JsonDocument.Parse(await periods.Content.ReadAsStringAsync());
        periodsDoc.RootElement.EnumerateArray()
            .Any(p => p.GetProperty("effectiveFrom").GetString()!.StartsWith(from, StringComparison.Ordinal))
            .Should().BeTrue();

        var exact = await _client.GetAsync($"/api/registers/work-costs?effectiveFrom={from}");
        exact.StatusCode.Should().Be(HttpStatusCode.OK);
        using var exactDoc = JsonDocument.Parse(await exact.Content.ReadAsStringAsync());
        exactDoc.RootElement.EnumerateArray().Should().OnlyContain(r =>
            r.GetProperty("ratePerHour").ValueKind == JsonValueKind.Null
            || r.GetProperty("ratePerHour").GetDecimal() == 77.7m);

        var deleted = await _client.DeleteAsync($"/api/registers/work-costs/periods?effectiveFrom={from}");
        deleted.StatusCode.Should().Be(HttpStatusCode.OK);

        var gone = await _client.DeleteAsync($"/api/registers/work-costs/periods?effectiveFrom={from}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePeriod_AsObserver_ReturnsForbidden()
    {
        if (!_sqlAvailable) return;

        using var observer = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var login = await observer.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "observer", password = "observer" });
        if (login.StatusCode != HttpStatusCode.OK)
            return;

        var response = await observer.DeleteAsync("/api/registers/work-costs/periods?effectiveFrom=2099-01-01");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string ResolveDownloadFileName(HttpResponseMessage response)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        var raw = disposition?.FileNameStar
            ?? disposition?.FileName
            ?? (response.Content.Headers.TryGetValues("Content-Disposition", out var values)
                ? string.Join(" ", values)
                : string.Empty);

        raw = raw.Trim().Trim('"');
        const string utf8Prefix = "UTF-8''";
        var idx = raw.IndexOf(utf8Prefix, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
            raw = raw[(idx + utf8Prefix.Length)..];

        try
        {
            return Uri.UnescapeDataString(raw.Trim('"'));
        }
        catch (UriFormatException)
        {
            return raw;
        }
    }
}
