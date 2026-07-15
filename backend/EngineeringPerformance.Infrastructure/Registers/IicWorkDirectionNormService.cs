using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Registers;

public sealed class IicWorkDirectionNormService : IIicNormService
{
    private readonly AppDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;

    public IicWorkDirectionNormService(AppDbContext context, ICacheInvalidator cacheInvalidator)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
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

        await ValidatePeriodAsync(request.WorkDirectionId, effectiveFrom, request.EffectiveTo, null, cancellationToken);
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
        await _cacheInvalidator.InvalidateAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<IicNormDto?> UpdateAsync(int id, IicNormWriteRequest request, CancellationToken cancellationToken = default)
    {
        var directionMap = await LoadDirectionMapAsync(cancellationToken);
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return null;

        var effectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc);

        await ValidatePeriodAsync(request.WorkDirectionId, effectiveFrom, request.EffectiveTo, id, cancellationToken);

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
        await _cacheInvalidator.InvalidateAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return false;

        _context.IicWorkDirectionNorms.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
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
        CancellationToken cancellationToken)
    {
        if (effectiveTo.HasValue && effectiveTo.Value.Date <= effectiveFrom.Date)
            throw new InvalidOperationException("EffectiveTo must be after EffectiveFrom.");

        var from = DateTime.SpecifyKind(effectiveFrom.Date, DateTimeKind.Utc);
        var to = effectiveTo.HasValue ? DateTime.SpecifyKind(effectiveTo.Value.Date, DateTimeKind.Utc) : (DateTime?)null;

        var query = _context.IicWorkDirectionNorms.AsNoTracking()
            .Where(n => n.WorkDirectionId == workDirectionId);

        if (excludeId.HasValue)
            query = query.Where(n => n.Id != excludeId.Value);

        var existing = await query.ToListAsync(cancellationToken);

        foreach (var norm in existing)
        {
            if (norm.EffectiveFrom.Date == from.Date)
                throw new InvalidOperationException("Norm with the same EffectiveFrom already exists for this work direction.");

            var normTo = norm.EffectiveTo ?? DateTime.MaxValue;
            var newTo = to ?? DateTime.MaxValue;

            if (from < normTo && norm.EffectiveFrom < newTo)
                throw new InvalidOperationException("Norm period overlaps an existing record for this work direction.");
        }
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

        return new IicNormDto(
            n.Id,
            n.WorkDirectionId,
            n.WorkDirection?.Name ?? direction?.Name,
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
