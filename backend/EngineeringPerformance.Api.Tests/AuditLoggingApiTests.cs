using System.Net;
using System.Net.Http.Json;

namespace EngineeringPerformance.Api.Tests;

public sealed class AuditLoggingApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public AuditLoggingApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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

    private sealed record AuditLogItem(
        int Id,
        int UserId,
        string UserName,
        DateTime Timestamp,
        int ActionType,
        string? Description);
}
