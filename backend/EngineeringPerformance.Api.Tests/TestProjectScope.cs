using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;

namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// Disposable project for API integration tests.
/// Convention: never create versions on seed projects — always use this scope and DELETE on dispose.
/// Code prefix: <c>apitest-</c> (name: <c>API test …</c>).
/// </summary>
public sealed class TestProjectScope : IAsyncDisposable
{
    public const string CodePrefix = "apitest-";
    public const string NamePrefix = "API test ";

    private readonly HttpClient _client;
    private bool _disposed;

    private TestProjectScope(HttpClient client, int id, string code, string name)
    {
        _client = client;
        Id = id;
        Code = code;
        Name = name;
    }

    public int Id { get; }
    public string Code { get; }
    public string Name { get; }

    public static async Task<TestProjectScope> CreateAsync(
        HttpClient client,
        string? nameHint = null,
        CancellationToken cancellationToken = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var code = $"{CodePrefix}{suffix}";
        var name = $"{NamePrefix}{nameHint ?? suffix}";

        var response = await client.PostAsJsonAsync(
            "/api/projects",
            new { code, name, workStart = (string?)null, workNumber = (string?)null },
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to create disposable test project: {(int)response.StatusCode} {body}");
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedProjectDto>(cancellationToken)
            ?? throw new InvalidOperationException("Empty create-project response.");

        return new TestProjectScope(client, created.Id, created.Code, created.Name);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                var response = await _client.DeleteAsync($"/api/projects/{Id}", cts.Token);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                    return;
            }
            catch
            {
                // Retry / fall through to SQL cleanup.
            }

            await Task.Delay(TimeSpan.FromSeconds(1 + attempt));
        }

        try
        {
            await HardDeleteViaSqlAsync();
        }
        catch
        {
            // Best-effort cleanup; do not mask the original test failure.
        }
    }

    private async Task HardDeleteViaSqlAsync()
    {
        var cs = Environment.GetEnvironmentVariable("EP_TEST_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? ApiWebApplicationFactory.ResolveDefaultConnectionString();

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 120;
        cmd.CommandText = """
            SET XACT_ABORT ON;
            SET QUOTED_IDENTIFIER ON;
            BEGIN TRAN;
            UPDATE dbo.ProjectPhysicalVolumes SET ParentId = NULL WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectAnalysisVersions WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectPhysicalVolumes WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectEquipments WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectLaborIntensities WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectPermissions WHERE ProjectId = @id;
            DELETE FROM dbo.ProjectCharacteristicSelections WHERE ProjectId = @id;
            DELETE FROM dbo.DataImportIterations WHERE ProjectId = @id;
            DELETE FROM dbo.Projects WHERE Id = @id;
            COMMIT;
            """;
        cmd.Parameters.AddWithValue("@id", Id);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed record CreatedProjectDto(int Id, string Code, string Name);
}
