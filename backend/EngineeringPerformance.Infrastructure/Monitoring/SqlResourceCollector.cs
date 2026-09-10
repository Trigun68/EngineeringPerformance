using EngineeringPerformance.Application.Monitoring;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public sealed class SqlResourceCollector(
    IConfiguration configuration,
    ILogger<SqlResourceCollector> logger)
{
    private const string Query = """
        SET NOCOUNT ON;
        SELECT cpu_count FROM sys.dm_os_sys_info;
        SELECT physical_memory_in_use_kb FROM sys.dm_os_process_memory;
        SELECT TOP (1)
          CAST(record AS xml).value('(./Record/SchedulerMonitorEvent/SystemHealth/ProcessUtilization)[1]', 'int') AS ProcessUtilization
        FROM sys.dm_os_ring_buffers
        WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
          AND record LIKE N'%ProcessUtilization%'
        ORDER BY timestamp DESC;
        SELECT
          SUM(CASE WHEN program_name = N'EP-Interactive' THEN 1 ELSE 0 END) AS Interactive,
          SUM(CASE WHEN program_name = N'EP-Analytics' THEN 1 ELSE 0 END) AS Analytics,
          SUM(CASE WHEN ISNULL(program_name, N'') NOT IN (N'EP-Interactive', N'EP-Analytics', N'EP-ResourceMonitor') THEN 1 ELSE 0 END) AS Other
        FROM sys.dm_exec_sessions
        WHERE is_user_process = 1;
        SELECT ISNULL(SUM(runnable_tasks_count), 0) AS Runnable
        FROM sys.dm_os_schedulers
        WHERE status = N'VISIBLE ONLINE';
        SELECT RTRIM(counter_name) AS counter_name, cntr_value
        FROM sys.dm_os_performance_counters
        WHERE counter_name IN (N'Batch Requests/sec', N'Buffer cache hit ratio', N'Buffer cache hit ratio base');
        SELECT COUNT(*) FROM dbo.RefreshTokens WHERE RevokedAt IS NULL AND ExpiresAt > SYSUTCDATETIME();
        """;

    private long _previousBatchRequests;
    private DateTime _previousUtc = DateTime.MinValue;
    private bool _hasBaseline;

    internal async Task<SqlSnapshot> CaptureAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var baseCs = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(baseCs))
            return SqlSnapshot.Unavailable("Not configured");

        try
        {
            var cs = SqlConnectionStringLane.ForResourceMonitor(baseCs);
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = Query;
            command.CommandTimeout = 2;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var cpuCount = await ReadIntAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var memoryKb = await ReadInt64Async(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var processPercent = await ReadNullableIntAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var (interactive, analytics, other) = await ReadLanesAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var runnable = await ReadIntAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var counters = await ReadCountersAsync(reader, cancellationToken);
            await reader.NextResultAsync(cancellationToken);
            var sessions = await ReadIntAsync(reader, cancellationToken);

            counters.TryGetValue("Batch Requests/sec", out var batchCumulative);
            counters.TryGetValue("Buffer cache hit ratio", out var hit);
            counters.TryGetValue("Buffer cache hit ratio base", out var hitBase);

            var batchPerSec = 0d;
            if (_hasBaseline && nowUtc > _previousUtc)
            {
                var elapsed = (nowUtc - _previousUtc).TotalSeconds;
                if (elapsed > 0 && batchCumulative >= _previousBatchRequests)
                    batchPerSec = (batchCumulative - _previousBatchRequests) / elapsed;
            }

            _previousBatchRequests = batchCumulative;
            _previousUtc = nowUtc;
            _hasBaseline = true;

            var hitRatio = hitBase > 0 ? hit / (double)hitBase : 0d;
            var cpuCores = ResourceCpu.CoresFromSqlProcessPercent(processPercent ?? 0, cpuCount);

            return new SqlSnapshot(
                true,
                "Connected",
                cpuCores,
                ResourceCpu.BytesToMib(memoryKb * 1024),
                interactive,
                analytics,
                other,
                runnable,
                ResourceCpu.Round(batchPerSec),
                ResourceCpu.Round(hitRatio),
                sessions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось снять SQL DMV для вкладки ресурсов");
            return SqlSnapshot.Unavailable(ex.Message);
        }
    }

    private static async Task<int> ReadIntAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
            return 0;
        return Convert.ToInt32(reader.GetValue(0));
    }

    private static async Task<int?> ReadNullableIntAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
            return null;
        return Convert.ToInt32(reader.GetValue(0));
    }

    private static async Task<long> ReadInt64Async(SqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(0))
            return 0;
        return Convert.ToInt64(reader.GetValue(0));
    }

    private static async Task<(int Interactive, int Analytics, int Other)> ReadLanesAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken))
            return (0, 0, 0);

        int Col(int i) => reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i));
        return (Col(0), Col(1), Col(2));
    }

    private static async Task<Dictionary<string, long>> ReadCountersAsync(
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
                continue;
            map[reader.GetString(0).Trim()] = Convert.ToInt64(reader.GetValue(1));
        }

        return map;
    }
}

internal sealed record SqlSnapshot(
    bool Available,
    string Status,
    double CpuCores,
    double RssMib,
    int Interactive,
    int Analytics,
    int Other,
    int Runnable,
    double BatchRequestsPerSec,
    double BufferCacheHitRatio,
    int ActiveSessions)
{
    public static SqlSnapshot Unavailable(string status)
        => new(false, status, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}
