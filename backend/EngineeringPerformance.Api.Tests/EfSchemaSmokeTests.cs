using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// E3: модель EF совпадает с INFORMATION_SCHEMA живой БД (db-init).
/// </summary>
public sealed class EfSchemaSmokeTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly bool _sqlAvailable;

    public EfSchemaSmokeTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        using var client = factory.CreateClient();
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(client);
    }

    [Fact]
    public async Task MappedTablesAndColumns_ExistInInformationSchema()
    {
        if (!_sqlAvailable)
            return;

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Как остальные Api.Tests: вне CI — early return, без Skip-атрибута (xunit 2.9.2).
        if (!await db.Database.CanConnectAsync())
            return;

        var tables = await LoadInformationSchemaTablesAsync(db);
        var columns = await LoadInformationSchemaColumnsAsync(db);
        var mismatches = new List<string>();

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;

            var store = ResolveStoreObject(entityType);
            if (store is null)
                continue;

            var schema = store.Value.Schema ?? "dbo";
            var tableName = store.Value.Name;
            var entityName = entityType.ClrType?.Name ?? entityType.Name;
            if (!tables.Contains((schema, tableName)))
            {
                mismatches.Add($"нет таблицы {schema}.{tableName} (сущность {entityName})");
                continue;
            }

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(store.Value);
                if (string.IsNullOrEmpty(columnName))
                    continue;

                if (!columns.Contains((schema, tableName, columnName)))
                {
                    mismatches.Add(
                        $"нет колонки {schema}.{tableName}.{columnName} (сущность {entityName}.{property.Name})");
                }
            }
        }

        Assert.True(
            mismatches.Count == 0,
            "Расхождение EF-модели и INFORMATION_SCHEMA:" + Environment.NewLine
            + string.Join(Environment.NewLine, mismatches.OrderBy(x => x, StringComparer.Ordinal)));
    }

    private static StoreObjectIdentifier? ResolveStoreObject(IReadOnlyEntityType entityType)
    {
        var tableName = entityType.GetTableName();
        if (!string.IsNullOrEmpty(tableName))
            return StoreObjectIdentifier.Table(tableName, entityType.GetSchema() ?? "dbo");

        var viewName = entityType.GetViewName();
        if (!string.IsNullOrEmpty(viewName))
            return StoreObjectIdentifier.View(viewName, entityType.GetViewSchema() ?? "dbo");

        return null;
    }

    private static async Task<HashSet<(string Schema, string Name)>> LoadInformationSchemaTablesAsync(AppDbContext db)
    {
        var result = new HashSet<(string Schema, string Name)>();
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await db.Database.OpenConnectionAsync();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT TABLE_SCHEMA, TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE IN ('BASE TABLE', 'VIEW')
                """;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetString(1)));
        }
        finally
        {
            if (shouldClose)
                await db.Database.CloseConnectionAsync();
        }

        return result;
    }

    private static async Task<HashSet<(string Schema, string Name, string Column)>> LoadInformationSchemaColumnsAsync(
        AppDbContext db)
    {
        var result = new HashSet<(string Schema, string Name, string Column)>();
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await db.Database.OpenConnectionAsync();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME
                FROM INFORMATION_SCHEMA.COLUMNS
                """;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        finally
        {
            if (shouldClose)
                await db.Database.CloseConnectionAsync();
        }

        return result;
    }
}
