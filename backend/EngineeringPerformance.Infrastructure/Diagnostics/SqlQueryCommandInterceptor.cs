using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EngineeringPerformance.Application.Monitoring;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Diagnostics;

public sealed class SqlProfilingOptions
{
    public const string SectionName = "SqlProfiling";

    // По умолчанию выключено: замер и persist метрик добавляют overhead на каждый запрос.
    // Включать точечно через SqlProfiling__Enabled=true при расследовании производительности.
    public bool Enabled { get; set; }

    /// <summary>Не писать в БД команды короче порога (мс). 0 = писать все.</summary>
    public double MinDurationMsToPersist { get; set; } = 50;

    /// <summary>Таймаут INSERT метрик. Не должен конкурировать с пользовательским SQL.</summary>
    public int PersistCommandTimeoutSeconds { get; set; } = 8;

    public int RetentionDays { get; set; } = 14;

    public int MaxParametersLength { get; set; } = 4000;

    public int MaxSqlPreviewLength { get; set; } = 2000;
}

/// <summary>Замер длительности SQL-команд EF Core внутри приложения.</summary>
public sealed class SqlQueryCommandInterceptor : DbCommandInterceptor
{
    private static readonly Regex TableRegex = new(
        @"(?:FROM|JOIN|INTO|UPDATE|DELETE\s+FROM)\s+\[?(?:dbo\]\.)?\[?([A-Za-z_][A-Za-z0-9_]*)\]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SqlProfilingOptions _options;

    public SqlQueryCommandInterceptor(IOptions<SqlProfilingOptions> options)
    {
        _options = options.Value;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        // Длительность чтения — в DataReaderDisposing.
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult DataReaderDisposing(
        DbCommand command,
        DataReaderDisposingEventData eventData,
        InterceptionResult result)
    {
        Record(command, eventData.Duration.TotalMilliseconds, rowsReturned: eventData.ReadCount, rowsAffected: null);
        return base.DataReaderDisposing(command, eventData, result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Record(command, eventData.Duration.TotalMilliseconds, rowsReturned: null, rowsAffected: result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration.TotalMilliseconds, rowsReturned: null, rowsAffected: result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        Record(command, eventData.Duration.TotalMilliseconds, rowsReturned: 1, rowsAffected: null);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Record(command, eventData.Duration.TotalMilliseconds, rowsReturned: 1, rowsAffected: null);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command, double durationMs, int? rowsReturned, int? rowsAffected)
    {
        if (!_options.Enabled)
            return;

        var scope = SqlProfilingScope.Current;
        if (scope is null || scope.SuppressPersistence)
            return;

        var sql = command.CommandText ?? string.Empty;
        if (IsInternalMetricsSql(sql))
            return;

        if (durationMs < _options.MinDurationMsToPersist)
            return;

        var queryType = DetectQueryType(sql);
        var tables = ExtractTables(sql);
        var normalized = NormalizeSql(sql);
        var hash = ComputeHash(normalized);

        scope.Add(new SqlCommandMetricDraft
        {
            TimestampUtc = DateTime.UtcNow,
            QueryType = queryType,
            TablesInvolved = tables,
            DurationMs = Math.Round(durationMs, 4),
            RowsReturned = rowsReturned,
            RowsAffected = rowsAffected,
            QueryHash = hash,
            SqlPreview = Truncate(normalized, _options.MaxSqlPreviewLength),
            ParametersJson = SerializeParameters(command, _options.MaxParametersLength),
        });
    }

    public static bool IsInternalMetricsSql(string sql) =>
        sql.Contains("SqlQueryMetrics", StringComparison.OrdinalIgnoreCase);

    public static string DetectQueryType(string sql)
    {
        var span = sql.AsSpan().TrimStart();
        if (span.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            || span.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            return "SELECT";
        if (span.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            return "INSERT";
        if (span.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            return "UPDATE";
        if (span.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            return "DELETE";
        if (span.StartsWith("MERGE", StringComparison.OrdinalIgnoreCase))
            return "MERGE";
        if (span.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase)
            || span.StartsWith("EXECUTE", StringComparison.OrdinalIgnoreCase))
            return "EXEC";
        return "OTHER";
    }

    public static string? ExtractTables(string sql)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TableRegex.Matches(sql))
        {
            if (match.Groups.Count > 1)
                names.Add(match.Groups[1].Value);
        }

        return names.Count == 0 ? null : string.Join(',', names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    public static string NormalizeSql(string sql)
    {
        var collapsed = Regex.Replace(sql, @"\s+", " ").Trim();
        return collapsed;
    }

    public static string ComputeHash(string normalizedSql)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSql));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? SerializeParameters(DbCommand command, int maxLength)
    {
        if (command.Parameters.Count == 0)
            return null;

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DbParameter parameter in command.Parameters)
        {
            var name = string.IsNullOrEmpty(parameter.ParameterName) ? "?" : parameter.ParameterName;
            dict[name] = RedactParameter(name, parameter.Value);
        }

        var json = JsonSerializer.Serialize(dict);
        return Truncate(json, maxLength);
    }

    private static object? RedactParameter(string name, object? value)
    {
        if (value is null or DBNull)
            return null;

        if (name.Contains("password", StringComparison.OrdinalIgnoreCase)
            || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || name.Contains("token", StringComparison.OrdinalIgnoreCase))
            return "***";

        var text = value.ToString() ?? string.Empty;
        return text.Length > 200 ? text[..200] + "…" : value is string or int or long or decimal or double or float or bool or DateTime
            ? value
            : text;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null)
            return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
