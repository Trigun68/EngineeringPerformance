using System.Net;
using System.Net.Http.Json;

namespace EngineeringPerformance.Api.Tests;

internal static class IntegrationTestAuth
{
    public static async Task<bool> TryLoginAsAdminAsync(HttpClient client)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "admin", password = "admin" },
                cts.Token);

            if (response.StatusCode == HttpStatusCode.OK)
                return true;

            // Best-effort unlock when lockout was tripped by parallel/shared DB runs.
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                await Task.Delay(250, CancellationToken.None);
                var retry = await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { username = "admin", password = "admin" },
                    cts.Token);
                return retry.StatusCode == HttpStatusCode.OK;
            }

            return false;
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
        if (!available)
        {
            // Повтор после возможного lockout/MFA от параллельных прогонов на shared SQL.
            available = TryLoginAsAdminAsync(client).GetAwaiter().GetResult();
        }

        IntegrationTestSqlGate.RequireAvailable(available);
        return available;
    }
}
