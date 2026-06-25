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
}
