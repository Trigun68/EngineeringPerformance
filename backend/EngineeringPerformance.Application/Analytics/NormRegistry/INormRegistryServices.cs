namespace EngineeringPerformance.Application.Analytics.NormRegistry;

public interface INormHealthCheckService
{
    Task<HealthSummaryDto> RunAsync(
        int[]? groupIds = null,
        CancellationToken cancellationToken = default);
}

public interface INormUsageStatsService
{
    Task<NormUsageStatsDto> GetUsageAsync(int normId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, NormUsageHint>> GetHintsAsync(
        IReadOnlyCollection<int>? normIds = null,
        CancellationToken cancellationToken = default);
}

public interface INormRegistryService
{
    Task<PagedResultDto<NormListItemDto>> ListAsync(
        NormFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<NormDetailsDto?> GetDetailsAsync(int normId, CancellationToken cancellationToken = default);

    Task<HealthSummaryDto> HealthCheckAsync(
        int[]? groupIds = null,
        CancellationToken cancellationToken = default);

    Task<TimelineResponseDto> GetTimelineAsync(
        bool includeHistorical = true,
        bool includeFuture = true,
        CancellationToken cancellationToken = default);

    Task<NormRegistryFiltersMetaDto> GetFiltersMetaAsync(CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportRegistryAsync(
        NormFilterDto filter,
        string format,
        CancellationToken cancellationToken = default);

    Task<(byte[] Content, string ContentType, string FileName)> ExportHealthReportAsync(
        string format,
        int[]? groupIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Закрыть период нормы (archive). 409 если уже Historical.</summary>
    Task<NormListItemDto> ArchiveAsync(
        int normId,
        int userId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
}
