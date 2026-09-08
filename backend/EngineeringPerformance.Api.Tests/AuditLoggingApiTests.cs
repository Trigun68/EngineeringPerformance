using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.Api.Tests;

public sealed class AuditLoggingApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public AuditLoggingApiTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task Login_WritesLoginEntryToAuditLog()
    {
        if (!_sqlAvailable)
            return;

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "admin" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var logsResponse = await _client.GetAsync("/api/audit/user-actions?actionType=Login&take=5");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<AuditLogItem>>();
        Assert.NotNull(logs);
        var nonNullLogs = logs!;
        Assert.Contains(nonNullLogs, l => l.UserName == "admin" && (l.Description?.Contains("Manual login") ?? false));
    }

    [Fact]
    public async Task CreateProjectVersion_WritesVersionCreateAuditRow()
    {
        if (!_sqlAvailable)
            return;

        var catalogTypeId = await ResolveKfoTypeIdAsync();
        Assert.True(catalogTypeId is not null, "В seed нет типа каталога КФО.");

        await using var project = await TestProjectScope.CreateAsync(_client, "audit-ver");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions",
            new
            {
                versionName = $"audit-ver-{suffix}",
                name = $"audit-ver-{suffix}",
                direction = (int)AnalysisDirection.Plan,
                physicalVolumeTypeId = catalogTypeId.Value,
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var versionId = created.RootElement.GetProperty("id").GetInt32();

        try
        {
            var log = await FindLogAsync(
                UserActionType.VersionCreate,
                "ProjectAnalysisVersion",
                versionId);
            Assert.True(
                log is not null,
                $"Нет UserActionLogs VersionCreate для ProjectAnalysisVersion/{versionId}.");
            Assert.Equal("ProjectAnalysisVersion", log!.EntityType);
            Assert.Equal(versionId, log.EntityId);
        }
        finally
        {
            await DeleteLogsForEntityAsync("ProjectAnalysisVersion", versionId);
        }
    }

    [Fact]
    public async Task CreatePhysicalVolume_WritesPhysicalVolumeCreateAuditRow()
    {
        if (!_sqlAvailable)
            return;

        var catalogTypeId = await ResolveKfoTypeIdAsync();
        Assert.True(catalogTypeId is not null, "В seed нет типа каталога КФО.");

        var catalogVolumeId = await TryFirstKfoCatalogIdAsync();
        Assert.True(catalogVolumeId is not null, "В справочнике КФО нет позиций для POST physical-volumes.");

        await using var project = await TestProjectScope.CreateAsync(_client, "audit-ppv");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var versionResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions",
            new
            {
                versionName = $"audit-ppv-{suffix}",
                name = $"audit-ppv-{suffix}",
                direction = (int)AnalysisDirection.Plan,
                physicalVolumeTypeId = catalogTypeId.Value,
            });
        Assert.Equal(HttpStatusCode.Created, versionResponse.StatusCode);
        using var versionDoc = JsonDocument.Parse(await versionResponse.Content.ReadAsStringAsync());
        var versionId = versionDoc.RootElement.GetProperty("id").GetInt32();

        var ppvResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/versions/{versionId}/physical-volumes",
            new { physicalVolumeId = catalogVolumeId.Value, quantity = 1m, workName = "audit-ppv" });
        Assert.Equal(HttpStatusCode.Created, ppvResponse.StatusCode);
        using var ppvDoc = JsonDocument.Parse(await ppvResponse.Content.ReadAsStringAsync());
        var ppvId = ppvDoc.RootElement.GetProperty("id").GetInt32();

        try
        {
            var log = await FindLogAsync(
                UserActionType.PhysicalVolumeCreate,
                "ProjectPhysicalVolume",
                ppvId);
            Assert.True(
                log is not null,
                $"Нет UserActionLogs PhysicalVolumeCreate для ProjectPhysicalVolume/{ppvId}.");
            Assert.Equal("ProjectPhysicalVolume", log!.EntityType);
            Assert.Equal(ppvId, log.EntityId);
        }
        finally
        {
            await DeleteLogsForEntityAsync("ProjectPhysicalVolume", ppvId);
            await DeleteLogsForEntityAsync("ProjectAnalysisVersion", versionId);
        }
    }

    private async Task<AuditLogRow?> FindLogAsync(UserActionType actionType, string entityType, int entityId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actionName = actionType.ToString();
        return await db.UserActionLogs.AsNoTracking()
            .Where(l => l.ActionType == actionName
                         && l.EntityType == entityType
                         && l.EntityId == entityId)
            .OrderByDescending(l => l.Id)
            .Select(l => new AuditLogRow(l.Id, l.ActionType, l.EntityType, l.EntityId))
            .FirstOrDefaultAsync();
    }

    private async Task DeleteLogsForEntityAsync(string entityType, int entityId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.UserActionLogs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .ToListAsync();
        if (rows.Count == 0)
            return;

        db.UserActionLogs.RemoveRange(rows);
        await db.SaveChangesAsync();
    }

    private async Task<int?> ResolveKfoTypeIdAsync()
    {
        var dict = await _client.GetAsync("/api/dictionaries/all");
        if (!dict.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(await dict.Content.ReadAsStringAsync());
        foreach (var item in doc.RootElement.GetProperty("physicalVolumeTypes").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == "КФО")
                return item.GetProperty("id").GetInt32();
        }

        return null;
    }

    private async Task<int?> TryFirstKfoCatalogIdAsync()
    {
        var list = await _client.GetAsync("/api/directories/physical-volumes-kfo?page=1&pageSize=1");
        if (!list.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("items", out var arr) ? arr : default;
        if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
            return null;

        return items[0].GetProperty("id").GetInt32();
    }

    private sealed record AuditLogItem(
        int Id,
        int UserId,
        string UserName,
        DateTime Timestamp,
        int ActionType,
        string? Description);

    private sealed record AuditLogRow(int Id, string ActionType, string? EntityType, int? EntityId);
}
