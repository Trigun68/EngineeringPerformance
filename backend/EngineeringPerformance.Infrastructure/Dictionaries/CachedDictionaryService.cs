using System.Text.Json;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly;
using StackExchange.Redis;

namespace EngineeringPerformance.Infrastructure.Dictionaries;

/// <summary>
/// Загрузка всех справочников с кэшем Redis/IDistributedCache и параллельной загрузкой из БД.
/// TTL кэша: 30 минут. Инвалидация при изменении данных через ICacheInvalidator.
/// При обрыве подключения к Redis - автоматический fallback к БД с retry.
/// </summary>
public sealed class CachedDictionaryService : IDictionaryService
{
    public const string CacheKey = "dictionaries:all:v3";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedDictionaryService> _logger;
    private readonly AsyncPolicy _redisRetryPolicy;

    public CachedDictionaryService(
        IDbContextFactory<AppDbContext> contextFactory,
        IDistributedCache cache,
        ILogger<CachedDictionaryService> logger)
    {
        _contextFactory = contextFactory;
        _cache = cache;
        _logger = logger;
        _redisRetryPolicy = Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, ctx) =>
                {
                    _logger.LogWarning(exception,
                        "Redis cache access failed. Retry {RetryCount} in {Delay}ms",
                        retryCount, timeSpan.TotalMilliseconds);
                });
    }

    public Task<DictionariesAllDto> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync(cancellationToken);

    public async Task<bool> UnitExistsAsync(int unitId, CancellationToken cancellationToken = default)
    {
        var all = await GetOrLoadAsync(cancellationToken);
        return all.UnitsOfMeasurement.Any(u => u.Id == unitId);
    }

    public async Task<bool> DirectionExistsAsync(int directionId, CancellationToken cancellationToken = default)
    {
        var all = await GetOrLoadAsync(cancellationToken);
        return all.IicWorkDirections.Any(d => d.Id == directionId);
    }

    public async Task<bool> NormTypeExistsAsync(string normType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(normType))
            return false;

        var token = normType.Trim();
        var all = await GetOrLoadAsync(cancellationToken);
        return all.PhysicalVolumeTypes.Any(t =>
            string.Equals(t.Code, token, StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.Name, token, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<DictionariesAllDto> GetOrLoadAsync(CancellationToken cancellationToken)
    {
        // Попытка получить из кэша с retry при ошибках подключения
        try
        {
            var cachedBytes = await _redisRetryPolicy.ExecuteAsync(async () =>
                await _cache.GetAsync(CacheKey, cancellationToken));

            if (cachedBytes is not null)
            {
                var cached = JsonSerializer.Deserialize<DictionariesAllDto>(cachedBytes);
                if (cached is not null)
                {
                    _logger.LogInformation("Dictionary cache HIT for {CacheKey}", CacheKey);
                    return cached;
                }
            }

            _logger.LogInformation("Dictionary cache MISS for {CacheKey}", CacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read from cache, falling back to database");
        }

        // Загрузка из БД (параллельно с отдельными контекстами)
        var dto = await LoadAllFromDbParallelAsync(cancellationToken);

        // Попытка записать в кэш (не критично если не получится)
        try
        {
            await _redisRetryPolicy.ExecuteAsync(async () =>
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                };
                await _cache.SetAsync(CacheKey, JsonSerializer.SerializeToUtf8Bytes(dto), options, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to cache, but returning data from database");
        }

        return dto;
    }

    /// <summary>
    /// Параллельная загрузка всех справочников из БД для максимальной производительности.
    /// Каждый запрос использует отдельный DbContext (thread-safe).
    /// </summary>
    private async Task<DictionariesAllDto> LoadAllFromDbParallelAsync(
        CancellationToken cancellationToken)
    {
        // Создаем отдельный контекст для каждого параллельного запроса
        var volumeTypesTask = LoadWithNewContextAsync(LoadVolumeTypesAsync, cancellationToken);
        var volumesKfoTask = LoadWithNewContextAsync(LoadVolumesKfoAsync, cancellationToken);
        var volumesBerTask = LoadWithNewContextAsync(LoadVolumesBerAsync, cancellationToken);
        var equipmentsTask = LoadWithNewContextAsync(LoadEquipmentsAsync, cancellationToken);
        var directionsTask = LoadWithNewContextAsync(LoadDirectionsAsync, cancellationToken);
        var unitsTask = LoadWithNewContextAsync(LoadUnitsAsync, cancellationToken);
        var laborTask = LoadWithNewContextAsync(LoadLaborIntensitiesAsync, cancellationToken);
        var normsTask = LoadWithNewContextAsync(LoadNormsAsync, cancellationToken);
        var characteristicsTask = LoadWithNewContextAsync(LoadCharacteristicsAsync, cancellationToken);

        await Task.WhenAll(
            volumeTypesTask, volumesKfoTask, volumesBerTask, equipmentsTask,
            directionsTask, unitsTask, laborTask, normsTask, characteristicsTask
        );

        // Объединяем KFO и BER для legacy-ключа physicalVolumes (ID могут совпадать — не для FK).
        var volumes = volumesKfoTask.Result.Concat(volumesBerTask.Result).ToList();

        return new DictionariesAllDto(
            PhysicalVolumeTypes: volumeTypesTask.Result,
            PhysicalVolumes: volumes,
            PhysicalVolumesKfo: volumesKfoTask.Result,
            PhysicalVolumesBer: volumesBerTask.Result,
            Equipments: equipmentsTask.Result,
            LaborIntensities: laborTask.Result,
            IicWorkDirections: directionsTask.Result,
            IicWorkDirectionNorms: normsTask.Result,
            UnitsOfMeasurement: unitsTask.Result,
            ProjectCharacteristics: characteristicsTask.Result);
    }

    private async Task<List<DictionaryItemDto>> LoadWithNewContextAsync(
        Func<AppDbContext, CancellationToken, Task<List<DictionaryItemDto>>> loadFunc,
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await loadFunc(context, cancellationToken);
    }

    private static Task<List<DictionaryItemDto>> LoadVolumeTypesAsync(AppDbContext context, CancellationToken ct) =>
        context.PhysicalVolumeTypes.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, x.Code, x.Description, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadVolumesKfoAsync(AppDbContext context, CancellationToken ct) =>
        context.PhysicalVolumesKfo.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, x.Code, x.Description, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadVolumesBerAsync(AppDbContext context, CancellationToken ct) =>
        context.PhysicalVolumesBer.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, x.Code, x.Description, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadEquipmentsAsync(AppDbContext context, CancellationToken ct) =>
        context.Equipments.AsNoTracking()
            .Where(x => !x.IsArchived && x.ProjectId != null)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, null, x.Description, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadDirectionsAsync(AppDbContext context, CancellationToken ct) =>
        context.IicWorkDirections.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, x.Code, x.Description, x.ParentId, x.UnitId))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadUnitsAsync(AppDbContext context, CancellationToken ct) =>
        context.UnitsOfMeasurement.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, null, x.Description, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadLaborIntensitiesAsync(AppDbContext context, CancellationToken ct) =>
        context.LaborIntensities.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, x.Code, null, x.ParentId, null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadNormsAsync(AppDbContext context, CancellationToken ct) =>
        context.IicWorkDirectionNorms.AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new DictionaryItemDto(
                x.Id,
                x.WorkDirectionId.ToString(),
                null,
                x.EffectiveFrom.ToString("yyyy-MM-dd"),
                x.WorkDirectionId,
                null))
            .ToListAsync(ct);

    private static Task<List<DictionaryItemDto>> LoadCharacteristicsAsync(AppDbContext context, CancellationToken ct) =>
        context.ProjectCharacteristics.AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DictionaryItemDto(x.Id, x.Name, null, null, x.ParentId, null))
            .ToListAsync(ct);
}
