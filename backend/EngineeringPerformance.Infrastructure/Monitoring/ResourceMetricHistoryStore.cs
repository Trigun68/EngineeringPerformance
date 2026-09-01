using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Monitoring;

public interface IResourceMetricHistoryStore
{
    Task AppendAsync(ResourceSample sample, CancellationToken cancellationToken);

    Task<IReadOnlyList<ResourceSample>> GetBucketedAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int bucketSeconds,
        CancellationToken cancellationToken);

    Task<DateTime?> GetOldestTimestampUtcAsync(CancellationToken cancellationToken);

    Task PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken);
}

public sealed class ResourceMetricHistoryStore(
    IConfiguration configuration,
    ILogger<ResourceMetricHistoryStore> logger) : IResourceMetricHistoryStore
{
    private static readonly DateTime EpochUtc = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task AppendAsync(ResourceSample sample, CancellationToken cancellationToken)
    {
        var cs = MonitorConnectionString();
        if (cs is null)
            return;

        try
        {
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 2;
            command.CommandText = """
                INSERT INTO dbo.ResourceMetricSamples (
                    TimestampUtc, ActiveUsers, ActiveSessions, KestrelConnections, CurrentHttpRequests,
                    SqlInteractive, SqlAnalytics, SqlOther, RedisClients, AnalyticsGateBusy, AnalyticsGateLimit,
                    BackendAvailable, BackendCpuCores, BackendRssMib,
                    RedisAvailable, RedisStatus, RedisCpuCores, RedisRssMib,
                    SqlAvailable, SqlStatus, SqlCpuCores, SqlRssMib,
                    GcHeapMib, AllocRateBytesPerSecond, Gen2Collections, ThreadPoolBusy, ThreadPoolQueue,
                    SqlRunnable, RedisHitRatio, RedisOpsPerSec, RedisEvictedKeys, RedisFragmentation,
                    SqlBatchRequestsPerSec, SqlBufferCacheHitRatio)
                VALUES (
                    @TimestampUtc, @ActiveUsers, @ActiveSessions, @KestrelConnections, @CurrentHttpRequests,
                    @SqlInteractive, @SqlAnalytics, @SqlOther, @RedisClients, @AnalyticsGateBusy, @AnalyticsGateLimit,
                    @BackendAvailable, @BackendCpuCores, @BackendRssMib,
                    @RedisAvailable, @RedisStatus, @RedisCpuCores, @RedisRssMib,
                    @SqlAvailable, @SqlStatus, @SqlCpuCores, @SqlRssMib,
                    @GcHeapMib, @AllocRateBytesPerSecond, @Gen2Collections, @ThreadPoolBusy, @ThreadPoolQueue,
                    @SqlRunnable, @RedisHitRatio, @RedisOpsPerSec, @RedisEvictedKeys, @RedisFragmentation,
                    @SqlBatchRequestsPerSec, @SqlBufferCacheHitRatio)
                """;
            AddSampleParameters(command, sample);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            // Дубликат TimestampUtc — игнорируем.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать сэмпл ресурсов в SQL");
        }
    }

    public async Task<IReadOnlyList<ResourceSample>> GetBucketedAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int bucketSeconds,
        CancellationToken cancellationToken)
    {
        var cs = MonitorConnectionString();
        if (cs is null)
            return [];

        var bucket = Math.Max(1, bucketSeconds);
        try
        {
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 15;
            command.CommandText = """
                SELECT
                    DATEADD(second, (DATEDIFF(second, @Epoch, TimestampUtc) / @Bucket) * @Bucket, @Epoch) AS BucketUtc,
                    MAX(ActiveUsers), MAX(ActiveSessions), MAX(KestrelConnections), MAX(CurrentHttpRequests),
                    MAX(SqlInteractive), MAX(SqlAnalytics), MAX(SqlOther), MAX(RedisClients),
                    MAX(AnalyticsGateBusy), MAX(AnalyticsGateLimit),
                    MAX(CAST(BackendAvailable AS int)), AVG(BackendCpuCores), AVG(BackendRssMib),
                    MAX(CAST(RedisAvailable AS int)), MAX(RedisStatus), AVG(RedisCpuCores), AVG(RedisRssMib),
                    MAX(CAST(SqlAvailable AS int)), MAX(SqlStatus), AVG(SqlCpuCores), AVG(SqlRssMib),
                    AVG(GcHeapMib), AVG(AllocRateBytesPerSecond), MAX(Gen2Collections),
                    MAX(ThreadPoolBusy), MAX(ThreadPoolQueue), MAX(SqlRunnable),
                    AVG(RedisHitRatio), AVG(RedisOpsPerSec), MAX(RedisEvictedKeys), AVG(RedisFragmentation),
                    AVG(SqlBatchRequestsPerSec), AVG(SqlBufferCacheHitRatio)
                FROM dbo.ResourceMetricSamples
                WHERE TimestampUtc >= @FromUtc AND TimestampUtc <= @ToUtc
                GROUP BY DATEADD(second, (DATEDIFF(second, @Epoch, TimestampUtc) / @Bucket) * @Bucket, @Epoch)
                ORDER BY BucketUtc
                """;
            command.Parameters.AddWithValue("@Epoch", EpochUtc);
            command.Parameters.AddWithValue("@Bucket", bucket);
            command.Parameters.AddWithValue("@FromUtc", fromUtc);
            command.Parameters.AddWithValue("@ToUtc", toUtc);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<ResourceSample>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(ReadBucket(reader));
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось прочитать историю ресурсов из SQL");
            return [];
        }
    }

    public async Task<DateTime?> GetOldestTimestampUtcAsync(CancellationToken cancellationToken)
    {
        var cs = MonitorConnectionString();
        if (cs is null)
            return null;

        try
        {
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 5;
            command.CommandText = "SELECT MIN(TimestampUtc) FROM dbo.ResourceMetricSamples";
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is null or DBNull)
                return null;
            return DateTime.SpecifyKind(Convert.ToDateTime(value), DateTimeKind.Utc);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось прочитать начало истории ресурсов");
            return null;
        }
    }

    public async Task PruneOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
    {
        var cs = MonitorConnectionString();
        if (cs is null)
            return;

        try
        {
            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 30;
            command.CommandText = "DELETE FROM dbo.ResourceMetricSamples WHERE TimestampUtc < @Cutoff";
            command.Parameters.AddWithValue("@Cutoff", cutoffUtc);
            var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
            if (deleted > 0)
                logger.LogInformation("Удалено {Count} устаревших сэмплов ресурсов", deleted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось очистить историю ресурсов старше {Cutoff}", cutoffUtc);
        }
    }

    private string? MonitorConnectionString()
    {
        var baseCs = configuration.GetConnectionString("DefaultConnection");
        return string.IsNullOrWhiteSpace(baseCs) ? null : SqlConnectionStringLane.ForResourceMonitor(baseCs);
    }

    private static void AddSampleParameters(SqlCommand command, ResourceSample sample)
    {
        command.Parameters.AddWithValue("@TimestampUtc", sample.TimestampUtc);
        command.Parameters.AddWithValue("@ActiveUsers", sample.ActiveUsers);
        command.Parameters.AddWithValue("@ActiveSessions", sample.ActiveSessions);
        command.Parameters.AddWithValue("@KestrelConnections", sample.KestrelConnections);
        command.Parameters.AddWithValue("@CurrentHttpRequests", sample.CurrentHttpRequests);
        command.Parameters.AddWithValue("@SqlInteractive", sample.SqlInteractive);
        command.Parameters.AddWithValue("@SqlAnalytics", sample.SqlAnalytics);
        command.Parameters.AddWithValue("@SqlOther", sample.SqlOther);
        command.Parameters.AddWithValue("@RedisClients", sample.RedisClients);
        command.Parameters.AddWithValue("@AnalyticsGateBusy", sample.AnalyticsGateBusy);
        command.Parameters.AddWithValue("@AnalyticsGateLimit", sample.AnalyticsGateLimit);
        command.Parameters.AddWithValue("@BackendAvailable", sample.BackendAvailable);
        command.Parameters.AddWithValue("@BackendCpuCores", sample.BackendCpuCores);
        command.Parameters.AddWithValue("@BackendRssMib", sample.BackendRssMib);
        command.Parameters.AddWithValue("@RedisAvailable", sample.RedisAvailable);
        command.Parameters.AddWithValue("@RedisStatus", (object?)Truncate(sample.RedisStatus, 200) ?? DBNull.Value);
        command.Parameters.AddWithValue("@RedisCpuCores", sample.RedisCpuCores);
        command.Parameters.AddWithValue("@RedisRssMib", sample.RedisRssMib);
        command.Parameters.AddWithValue("@SqlAvailable", sample.SqlAvailable);
        command.Parameters.AddWithValue("@SqlStatus", (object?)Truncate(sample.SqlStatus, 200) ?? DBNull.Value);
        command.Parameters.AddWithValue("@SqlCpuCores", sample.SqlCpuCores);
        command.Parameters.AddWithValue("@SqlRssMib", sample.SqlRssMib);
        command.Parameters.AddWithValue("@GcHeapMib", sample.GcHeapMib);
        command.Parameters.AddWithValue("@AllocRateBytesPerSecond", sample.AllocRateBytesPerSecond);
        command.Parameters.AddWithValue("@Gen2Collections", sample.Gen2Collections);
        command.Parameters.AddWithValue("@ThreadPoolBusy", sample.ThreadPoolBusy);
        command.Parameters.AddWithValue("@ThreadPoolQueue", sample.ThreadPoolQueue);
        command.Parameters.AddWithValue("@SqlRunnable", sample.SqlRunnable);
        command.Parameters.AddWithValue("@RedisHitRatio", sample.RedisHitRatio);
        command.Parameters.AddWithValue("@RedisOpsPerSec", sample.RedisOpsPerSec);
        command.Parameters.AddWithValue("@RedisEvictedKeys", sample.RedisEvictedKeys);
        command.Parameters.AddWithValue("@RedisFragmentation", sample.RedisFragmentation);
        command.Parameters.AddWithValue("@SqlBatchRequestsPerSec", sample.SqlBatchRequestsPerSec);
        command.Parameters.AddWithValue("@SqlBufferCacheHitRatio", sample.SqlBufferCacheHitRatio);
    }

    private static ResourceSample ReadBucket(SqlDataReader reader)
        => new(
            DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc),
            Convert.ToInt32(reader.GetValue(1)),
            Convert.ToInt32(reader.GetValue(2)),
            Convert.ToInt32(reader.GetValue(3)),
            Convert.ToInt32(reader.GetValue(4)),
            Convert.ToInt32(reader.GetValue(5)),
            Convert.ToInt32(reader.GetValue(6)),
            Convert.ToInt32(reader.GetValue(7)),
            Convert.ToInt32(reader.GetValue(8)),
            Convert.ToInt32(reader.GetValue(9)),
            Convert.ToInt32(reader.GetValue(10)),
            Convert.ToInt32(reader.GetValue(11)) > 0,
            Convert.ToDouble(reader.GetValue(12)),
            Convert.ToDouble(reader.GetValue(13)),
            Convert.ToInt32(reader.GetValue(14)) > 0,
            reader.IsDBNull(15) ? null : reader.GetString(15),
            Convert.ToDouble(reader.GetValue(16)),
            Convert.ToDouble(reader.GetValue(17)),
            Convert.ToInt32(reader.GetValue(18)) > 0,
            reader.IsDBNull(19) ? null : reader.GetString(19),
            Convert.ToDouble(reader.GetValue(20)),
            Convert.ToDouble(reader.GetValue(21)),
            Convert.ToDouble(reader.GetValue(22)),
            Convert.ToDouble(reader.GetValue(23)),
            Convert.ToInt32(reader.GetValue(24)),
            Convert.ToInt32(reader.GetValue(25)),
            Convert.ToInt64(reader.GetValue(26)),
            Convert.ToInt32(reader.GetValue(27)),
            Convert.ToDouble(reader.GetValue(28)),
            Convert.ToDouble(reader.GetValue(29)),
            Convert.ToInt64(reader.GetValue(30)),
            Convert.ToDouble(reader.GetValue(31)),
            Convert.ToDouble(reader.GetValue(32)),
            Convert.ToDouble(reader.GetValue(33)));

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= max ? value : value[..max];
    }
}
