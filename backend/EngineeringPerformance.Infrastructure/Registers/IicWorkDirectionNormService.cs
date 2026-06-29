using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Registers;

public sealed class IicWorkDirectionNormService : IIicNormService
{
    private readonly AppDbContext _context;

    public IicWorkDirectionNormService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<IicNormDto>> GetAllAsync(DateTime? asOf, CancellationToken cancellationToken = default)
    {
        var date = DateTime.SpecifyKind((asOf ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var items = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .Where(n => n.EffectiveFrom <= date && (n.EffectiveTo == null || n.EffectiveTo >= date))
            .OrderBy(n => n.WorkDirectionId)
            .ThenByDescending(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        // На дату должен возвращаться один актуальный набор норм на направление.
        // Если в данных есть исторические пересечения, берём самую позднюю запись.
        var latestByDirection = items
            .GroupBy(x => x.WorkDirectionId)
            .Select(g => g
                .OrderByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.Id)
                .First())
            .OrderBy(x => x.WorkDirection?.SortOrder ?? int.MaxValue)
            .ThenBy(x => x.WorkDirection?.Name)
            .ToList();

        return latestByDirection.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<IicNormDto>> GetHistoryAsync(int workDirectionId, CancellationToken cancellationToken = default)
    {
        var items = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .Where(n => n.WorkDirectionId == workDirectionId)
            .OrderByDescending(n => n.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<IicNormDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.IicWorkDirectionNorms.AsNoTracking()
            .Include(n => n.WorkDirection)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        return item is null ? null : Map(item);
    }

    public async Task<IicNormDto> CreateAsync(IicNormWriteRequest request, CancellationToken cancellationToken = default)
    {
        await ValidatePeriodAsync(request.WorkDirectionId, request.EffectiveFrom, request.EffectiveTo, null, cancellationToken);

        var entity = new IicWorkDirectionNorm
        {
            WorkDirectionId = request.WorkDirectionId,
            Performance = request.Performance,
            Estimate = request.Estimate,
            StandardDeviation = request.StandardDeviation,
            UnitId = request.UnitId,
            EffectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc),
            EffectiveTo = request.EffectiveTo.HasValue
                ? DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc)
                : null,
            CreatedAt = DateTime.UtcNow,
        };

        _context.IicWorkDirectionNorms.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<IicNormDto?> UpdateAsync(int id, IicNormWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return null;

        await ValidatePeriodAsync(request.WorkDirectionId, request.EffectiveFrom, request.EffectiveTo, id, cancellationToken);

        entity.WorkDirectionId = request.WorkDirectionId;
        entity.Performance = request.Performance;
        entity.Estimate = request.Estimate;
        entity.StandardDeviation = request.StandardDeviation;
        entity.UnitId = request.UnitId;
        entity.EffectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc);
        entity.EffectiveTo = request.EffectiveTo.HasValue
            ? DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc)
            : null;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (request.History is { Count: > 0 })
            await SyncHistoryAsync(request.WorkDirectionId, request.History, id, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task SyncHistoryAsync(
        int workDirectionId,
        IReadOnlyList<IicNormHistoryItem> history,
        int currentId,
        CancellationToken cancellationToken)
    {
        var existing = await _context.IicWorkDirectionNorms
            .Where(n => n.WorkDirectionId == workDirectionId)
            .ToListAsync(cancellationToken);

        var incomingIds = history.Where(h => h.Id.HasValue).Select(h => h.Id!.Value).ToHashSet();
        incomingIds.Add(currentId);

        foreach (var row in existing.Where(e => !incomingIds.Contains(e.Id)))
            _context.IicWorkDirectionNorms.Remove(row);

        foreach (var row in history)
        {
            if (row.Id.HasValue)
            {
                var entity = existing.FirstOrDefault(e => e.Id == row.Id.Value);
                if (entity is null)
                    continue;

                await ValidatePeriodAsync(workDirectionId, row.EffectiveFrom, row.EffectiveTo, entity.Id, cancellationToken);
                entity.Performance = row.Performance;
                entity.Estimate = row.Estimate;
                entity.StandardDeviation = row.StandardDeviation;
                entity.UnitId = row.UnitId;
                entity.EffectiveFrom = DateTime.SpecifyKind(row.EffectiveFrom.Date, DateTimeKind.Utc);
                entity.EffectiveTo = row.EffectiveTo.HasValue
                    ? DateTime.SpecifyKind(row.EffectiveTo.Value.Date, DateTimeKind.Utc)
                    : null;
                entity.ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                await ValidatePeriodAsync(workDirectionId, row.EffectiveFrom, row.EffectiveTo, null, cancellationToken);
                _context.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
                {
                    WorkDirectionId = workDirectionId,
                    Performance = row.Performance,
                    Estimate = row.Estimate,
                    StandardDeviation = row.StandardDeviation,
                    UnitId = row.UnitId,
                    EffectiveFrom = DateTime.SpecifyKind(row.EffectiveFrom.Date, DateTimeKind.Utc),
                    EffectiveTo = row.EffectiveTo.HasValue
                        ? DateTime.SpecifyKind(row.EffectiveTo.Value.Date, DateTimeKind.Utc)
                        : null,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IicWorkDirectionNorms.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (entity is null)
            return false;

        _context.IicWorkDirectionNorms.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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
            var normTo = norm.EffectiveTo ?? DateTime.MaxValue;
            var newTo = to ?? DateTime.MaxValue;

            if (from < normTo && norm.EffectiveFrom < newTo)
                throw new InvalidOperationException("Norm period overlaps an existing record for this work direction.");
        }
    }

    private static IicNormDto Map(IicWorkDirectionNorm n) =>
        new(
            n.Id,
            n.WorkDirectionId,
            n.WorkDirection?.Name,
            n.Performance,
            n.Estimate,
            n.StandardDeviation,
            n.UnitId,
            n.EffectiveFrom,
            n.EffectiveTo,
            n.CreatedAt,
            n.ModifiedAt);
}
