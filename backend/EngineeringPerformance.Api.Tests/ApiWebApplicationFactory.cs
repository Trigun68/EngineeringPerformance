using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EngineeringPerformance.Infrastructure.Diagnostics;
using EngineeringPerformance.Infrastructure.Persistence;

namespace EngineeringPerformance.Api.Tests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<EngineeringPerformance.Api.Program>
{
    // Integration tests share the live Docker SQL DB with the :8081 stack.
    // Never create versions on seed projects — use TestProjectScope (apitest-*) and await using / finally DELETE.
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

            var connection = WithCommandTimeout(explicitConnection ?? ResolveDefaultConnectionString());

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Jwt:Secret"] = jwtSecret,
                ["Jwt:ExpirationMinutes"] = "60",
                ["Spnego:Enabled"] = "false",
                ["DevWindowsAuth:SimulatedWindowsUser"] = "",
            });
        });

        // Snapshot create/update and project cascade-delete need >30s on the shared Docker SQL.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContextFactory<AppDbContext>((sp, options) =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? ResolveDefaultConnectionString();
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(180);
                });
                options.AddInterceptors(sp.GetRequiredService<SqlQueryCommandInterceptor>());
            });
            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        // Snapshot rebuild / cascade delete on shared Docker SQL often exceeds the 100s default.
        client.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <summary>Resolves a working SQL connection string (same host discovery as the test factory).</summary>
    public static string ResolveDefaultConnectionString() => BuildDefaultConnectionString();

    private static string WithCommandTimeout(string connectionString)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
        {
            CommandTimeout = 120,
        };
        return builder.ConnectionString;
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
                return $"Server={server};Database=EngineeringPerformance;User Id=sa;Password={password};TrustServerCertificate=True;Command Timeout=120;";
            }
            catch
            {
                // Пробуем следующий хост.
            }
        }

        return $"Server=127.0.0.1,1433;Database=EngineeringPerformance;User Id=sa;Password={password};TrustServerCertificate=True;Command Timeout=120;";
    }
}
