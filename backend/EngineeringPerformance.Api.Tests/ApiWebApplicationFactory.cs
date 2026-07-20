using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EngineeringPerformance.Api.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<EngineeringPerformance.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var explicitConnection =
                Environment.GetEnvironmentVariable("EP_TEST_CONNECTION")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            var jwtSecret =
                Environment.GetEnvironmentVariable("Jwt__Secret")
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? "dev-jwt-secret-change-in-production-32chars";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    explicitConnection ?? BuildDefaultConnectionString(),
                ["Jwt:Secret"] = jwtSecret,
                ["Jwt:ExpirationMinutes"] = "60",
            });
        });
    }

    private static string BuildDefaultConnectionString()
    {
        var password = Environment.GetEnvironmentVariable("SQL_SA_PASSWORD")
            ?? "Your_strong_SA_password1!";
        var servers = new[] { "127.0.0.1,1433", "host.docker.internal,1433", "localhost,1433" };
        foreach (var server in servers)
        {
            try
            {
                using var conn = new Microsoft.Data.SqlClient.SqlConnection(
                    $"Server={server};Database=EngineeringPerformance;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=3");
                conn.Open();
                return $"Server={server};Database=EngineeringPerformance;User Id=sa;Password={password};TrustServerCertificate=True;";
            }
            catch
            {
                // Пробуем следующий хост.
            }
        }

        return $"Server=127.0.0.1,1433;Database=EngineeringPerformance;User Id=sa;Password={password};TrustServerCertificate=True;";
    }
}
