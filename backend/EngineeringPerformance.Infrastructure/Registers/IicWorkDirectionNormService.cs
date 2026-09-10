using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Registers;

public sealed class IicWorkDirectionNormService : IIicNormService
{
    private readonly AppDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;
    private readonly INormRegistryService _normRegistry;
    private readonly IFactNormCacheGeneration _factNormCache;
    private readonly IUserActionLogger _actionLogger;

    public IicWorkDirectionNormService(
        AppDbContext context,
        ICacheInvalidator cacheInvalidator,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
        INormRegistryService normRegistry,
        IFactNormCacheGeneration factNormCache,
        IUserActionLogger actionLogger)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _recalcOrchestrator = recalcOrchestrator;
        _normRegistry = normRegistry;
        _factNormCache = factNormCache;
        _actionLogger = actionLogger;
    }

    /// <summary>
    /// Сброс кэшей справочника и реестра норм. Аналитику проектов сбрасывает
    /// пересчёт затронутых версий (AfterNormChangedAsync), не InvalidateAll.
    /// </summary>
    private async Task InvalidateNormCachesAsync(CancellationToken cancellationToken)
    {
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        await _normRegistry.InvalidateCacheAsync(cancellationToken);
        await _factNormCache.BumpAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IicNormDto>> GetAllAsync(DateTime? asOf, CancellationToken cancellationToken = default)
    {
        var date = DateTime.SpecifyKind((asOf ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var items = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .Where(n => n.EffectiveFrom <= date && (n.EffectiveTo == null || n.EffectiveTo >= date))
            .OrderBy(n => n.WorkDirectionId)
            .ThenByDescending(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        // На дату должен возвращаться один актуальный набор норм на направление.
        var latestByDirection = items
            .GroupBy(x => x.WorkDirectionId)
            .Select(g => g
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.Id)
                .First())
            .OrderBy(x => x.WorkDirection?.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.WorkDirection?.Name)
            .ToList();

        return latestByDirection.Select(n => Map(n, directionMap)).ToList();
    }

    public async Task<IReadOnlyList<IicNormDto>> GetHistoryAsync(int workDirectionId, CancellationToken cancellationToken = default)
    {
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var items = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .Where(n => n.WorkDirectionId == workDirectionId)
            .OrderByDescending(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return items.Select(n => Map(n, directionMap)).ToList();
    }

    public async Task<IicNormDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var item = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        return item is null ? null : Map(item, directionMap);
    }

    public async Task<IicNormDto> CreateAsync(IicNormWriteRequest request, CancellationToken cancellationToken = default)
    {
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var effectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc);

        // Validate с учётом AutoClose: иначе открытый период (EffectiveTo=null) всегда «пересекается».
        await ValidatePeriodAsync(
            request.WorkDirectionId,
            effectiveFrom,
            request.EffectiveTo,
            excludeId: null,
            simulateAutoClose: true,
            cancellationToken);
        await AutoClosePreviousPeriodAsync(request.WorkDirectionId, effectiveFrom, null, cancellationToken);

        var resolvedUnitId = ResolveUnitId(request.WorkDirectionId, request.UnitId, directionMap);

        var entity = new IicWorkDirectionNorm
        {
            WorkDirectionId = request.WorkDirectionId,
            Performance = request.Performance,
            Estimate = request.Estimate,
            StandardDeviation = request.StandardDeviation,
            UnitId = resolvedUnitId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = request.EffectiveTo.HasValue
                ? DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc)
                : null,
            CreatedAt = DateTime.UtcNow,
        };

        NormDtoMapper.ApplyToEntity(entity, request);

        _context.IicWorkDirectionNorms.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        // IicNormCreate в enum нет — создание фиксируем как IicNormEdit.
        await _actionLogger.LogAsync(
            UserActionType.IicNormEdit,
            entityType: "IicWorkDirectionNorm",
            entityId: entity.Id,
            description: $"Created WorkDirectionId={entity.WorkDirectionId}",
            cancellationToken: cancellationToken);
        await InvalidateNormCachesAsync(cancellationToken);

        var recalcCount = await _recalcOrchestrator.AfterNormChangedAsync(
            entity.WorkDirectionId,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            cancellationToken);

        var dto = (await GetByIdAsync(entity.Id, cancellationToken))!;
        return dto with { RecalculatedVersionCount = recalcCount };
    }

    public async Task<IicNormDto?> UpdateAsync(int id, IicNormWriteRequest request, CancellationToken cancellationToken = default)
    {
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var effectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc);

        await ValidatePeriodAsync(
            request.WorkDirectionId,
            effectiveFrom,
            request.EffectiveTo,
            excludeId: id,
            simulateAutoClose: false,
            cancellationToken);

        entity.WorkDirectionId = request.WorkDirectionId;
        entity.Performance = request.Performance;
        entity.Estimate = request.Estimate;
        entity.StandardDeviation = request.StandardDeviation;
        entity.UnitId = ResolveUnitId(request.WorkDirectionId, request.UnitId, directionMap);
        entity.EffectiveFrom = effectiveFrom;
        entity.EffectiveTo = request.EffectiveTo.HasValue
            ? DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc)
            : null;
        entity.ModifiedAt = DateTime.UtcNow;
        NormDtoMapper.ApplyToEntity(entity, request);

        await _context.SaveChangesAsync(cancellationToken);
        await _actionLogger.LogAsync(
            UserActionType.IicNormEdit,
            entityType: "IicWorkDirectionNorm",
            entityId: entity.Id,
            description: $"Updated WorkDirectionId={entity.WorkDirectionId}",
            cancellationToken: cancellationToken);
        await InvalidateNormCachesAsync(cancellationToken);

        var recalcCount = await _recalcOrchestrator.AfterNormChangedAsync(
            entity.WorkDirectionId,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            cancellationToken);

        var dto = await GetByIdAsync(id, cancellationToken);
        return dto is null ? null : dto with { RecalculatedVersionCount = recalcCount };
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return false;

        var workDirectionId = entity.WorkDirectionId;
        var effectiveFrom = entity.EffectiveFrom;
        var effectiveTo = entity.EffectiveTo;

        _context.IicWorkDirectionNorms.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _actionLogger.LogAsync(
            UserActionType.IicNormEdit,
            entityType: "IicWorkDirectionNorm",
            entityId: id,
            description: $"Deleted WorkDirectionId={workDirectionId}",
            cancellationToken: cancellationToken);
        await InvalidateNormCachesAsync(cancellationToken);
        await _recalcOrchestrator.AfterNormChangedAsync(
            workDirectionId,
            effectiveFrom,
            effectiveTo,
            cancellationToken);
        return true;
    }

    private async Task AutoClosePreviousPeriodAsync(
        int workDirectionId,
        DateTime newEffectiveFrom,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _context.IicWorkDirectionNorms
            .Where(n => n.WorkDirectionId == workDirectionId);

        if (excludeId.HasValue)
            query = query.Where(n => n.Id != excludeId.Value);

        var openOrOverlapping = await query
            .Where(n => n.EffectiveTo == null || n.EffectiveTo >= newEffectiveFrom)
            .OrderByDescending(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var closeDate = newEffectiveFrom.AddDays(-1);
        foreach (var norm in openOrOverlapping)
        {
            if (norm.EffectiveFrom >= newEffectiveFrom)
                continue;

            if (norm.EffectiveTo is null || norm.EffectiveTo >= newEffectiveFrom)
            {
                norm.EffectiveTo = closeDate;
                norm.ModifiedAt = DateTime.UtcNow;
            }
        }
    }

    private async Task ValidatePeriodAsync(
        int workDirectionId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        int? excludeId,
        bool simulateAutoClose,
        CancellationToken cancellationToken)
    {
        if (effectiveTo.HasValue && effectiveTo.Value.Date <= effectiveFrom.Date)
            throw new InvalidOperationException("Дата «Действует по» должна быть позже «Действует с».");

        var from = DateTime.SpecifyKind(effectiveFrom.Date, DateTimeKind.Utc);
        var to = effectiveTo.HasValue ? DateTime.SpecifyKind(effectiveTo.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
        var closeDate = from.AddDays(-1);

        var query = _context.IicWorkDirectionNorms.AsNoTracking()
            .Where(n => n.WorkDirectionId == workDirectionId);

        if (excludeId.HasValue)
            query = query.Where(n => n.Id != excludeId.Value);

        var existing = await query.ToListAsync(cancellationToken);

        foreach (var norm in existing)
        {
            if (norm.EffectiveFrom.Date == from.Date)
                throw new InvalidOperationException(
                    "Норма с такой датой «Действует с» уже существует для этого направления.");

            // При создании AutoClose обрежет пересекающиеся/открытые периоды до from-1 —
            // валидация должна считать тот же итоговый интервал.
            var normTo = ResolveNormToForOverlap(norm, from, closeDate, simulateAutoClose);
            var newTo = to ?? DateTime.MaxValue;

            if (from < normTo && norm.EffectiveFrom < newTo)
                throw new InvalidOperationException(
                    "Период действия пересекается с существующей записью для этого направления.");
        }
    }

    /// <summary>
    /// Эффективный конец существующей записи для проверки пересечения.
    /// При create совпадает с результатом <see cref="AutoClosePreviousPeriodAsync"/>.
    /// </summary>
    private static DateTime ResolveNormToForOverlap(
        IicWorkDirectionNorm norm,
        DateTime newFrom,
        DateTime closeDate,
        bool simulateAutoClose)
    {
        if (simulateAutoClose
            && norm.EffectiveFrom < newFrom
            && (norm.EffectiveTo is null || norm.EffectiveTo >= newFrom))
        {
            return closeDate;
        }

        return norm.EffectiveTo ?? DateTime.MaxValue;
    }

    private async Task<IReadOnlyDictionary<int, IicWorkDirection>> LoadDirectionMapAsync(CancellationToken cancellationToken) =>
        await _context.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(x => !x.IsArchived)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

    private static int? ResolveUnitId(
        int workDirectionId,
        int? requestUnitId,
        IReadOnlyDictionary<int, IicWorkDirection> directionMap)
    {
        if (requestUnitId.HasValue)
            return requestUnitId;

        if (!directionMap.TryGetValue(workDirectionId, out var direction))
            return null;

        return IicWorkDirectionDisplayUnit.ResolveUnitId(direction, directionMap);
    }

    private static IicNormDto Map(IicWorkDirectionNorm n, IReadOnlyDictionary<int, IicWorkDirection> directionMap)
    {
        directionMap.TryGetValue(n.WorkDirectionId, out var direction);
        var resolvedUnitId = n.UnitId ?? (direction is not null
            ? IicWorkDirectionDisplayUnit.ResolveUnitId(direction, directionMap)
            : null);
        var unitName = direction is not null
            ? IicWorkDirectionDisplayUnit.ResolveUnitName(direction, directionMap)
            : null;
        var workDirectionName = direction is not null
            ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(direction, directionMap)
            : n.WorkDirection?.Name;

        return new IicNormDto(
            n.Id,
            n.WorkDirectionId,
            workDirectionName,
            n.WorkDirection?.Code ?? direction?.Code,
            n.Performance,
            n.Estimate,
            n.StandardDeviation,
            n.UnitId,
            resolvedUnitId,
            unitName,
            n.EffectiveFrom,
            n.EffectiveTo,
            n.CreatedAt,
            n.ModifiedAt,
            NormDtoMapper.ToNormFormulaDto(n),
            NormDtoMapper.ToDeviationFormulaDto(n),
            n.MigratedAt);
    }
}
