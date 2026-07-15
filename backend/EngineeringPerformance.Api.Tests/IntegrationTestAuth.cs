using System.Net;
using System.Net.Http.Json;

namespace EngineeringPerformance.Api.Tests;

internal static class IntegrationTestAuth
{
    public static async Task<bool> TryLoginAsAdminAsync(HttpClient client)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "admin", password = "admin" },
                cts.Token);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Логин + проверка доступности БД в CI (C-04).</summary>
    public static bool EnsureSqlAvailable(HttpClient client)
    {
        var available = TryLoginAsAdminAsync(client).GetAwaiter().GetResult();
        IntegrationTestSqlGate.RequireAvailable(available);
        return available;
    }
}
