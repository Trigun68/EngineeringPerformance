using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Registers;

public sealed class IicWorkDirectionWorkCostService(AppDbContext context) : IWorkCostService
{
    public async Task<IReadOnlyList<WorkCostRowDto>> GetAsOfAsync(
        DateTime? asOf,
        CancellationToken cancellationToken = default)
    {
        var date = DateTime.SpecifyKind((asOf ?? DateTime.UtcNow).Date, DateTimeKind.Utc);
        var directionMap = await LoadDirectionMapAsync(cancellationToken);

        var periods = await context.IicWorkDirectionWorkCosts.AsNoTracking()
            .Where(c => c.EffectiveFrom <= date && (c.EffectiveTo == null || c.EffectiveTo >= date))
            .OrderBy(c => c.WorkDirectionId)
            .ThenByDescending(c => c.EffectiveFrom)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        var latestByDirection = periods
            .GroupBy(c => c.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        return MapTree(directionMap, latestByDirection);
    }

    public async Task<IReadOnlyList<WorkCostRowDto>> GetByEffectiveFromAsync(
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        var from = DateOnlyUtc(effectiveFrom);
        var next = from.AddDays(1);
        var directionMap = await LoadDirectionMapAsync(cancellationToken);

        var periods = await context.IicWorkDirectionWorkCosts.AsNoTracking()
            .Where(c => c.EffectiveFrom >= from && c.EffectiveFrom < next)
            .OrderBy(c => c.WorkDirectionId)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        var byDirection = periods
            .GroupBy(c => c.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.First());

        return MapTree(directionMap, byDirection);
    }

    public async Task<IReadOnlyList<WorkCostPeriodDto>> ListPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await context.IicWorkDirectionWorkCosts.AsNoTracking()
            .Select(c => new { c.EffectiveFrom, c.EffectiveTo, c.RatePerHour })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(c => c.EffectiveFrom.Date)
            .OrderByDescending(g => g.Key)
            .Select(g =>
            {
                var tos = g.Select(x => x.EffectiveTo?.Date).Distinct().ToList();
                var varies = tos.Count > 1;
                var rates = g.Select(x => x.RatePerHour).Distinct().ToList();
                return new WorkCostPeriodDto(
                    DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                    varies ? null : g.Select(x => x.EffectiveTo).First(),
                    varies,
                    g.Count(),
                    rates.Count == 1 ? rates[0] : null);
            })
            .ToList();
    }

    public async Task<WorkCostBatchResult> SaveBatchAsync(
        WorkCostBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var effectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Date, DateTimeKind.Utc);
        var directions = await context.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived)
            .Select(d => new { d.Id, d.ParentId })
            .ToListAsync(cancellationToken);

        var nodes = directions
            .Select(d => new WorkCostDirectionNode(d.Id, d.ParentId))
            .ToList();
        var expanded = WorkCostCascade.Expand(nodes, request.Level, request.Items);
        if (expanded.Count == 0)
            throw new InvalidOperationException("Не задана ни одна ставка.");

        var targetIds = expanded.Keys.ToList();
        var existing = await context.IicWorkDirectionWorkCosts
            .Where(c => targetIds.Contains(c.WorkDirectionId))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var written = 0;

        foreach (var (directionId, rate) in expanded)
        {
            var forDir = existing.Where(c => c.WorkDirectionId == directionId).ToList();
            var sameFrom = forDir.FirstOrDefault(c => c.EffectiveFrom.Date == effectiveFrom.Date);
            if (sameFrom is not null)
            {
                sameFrom.RatePerHour = rate;
                sameFrom.ModifiedAt = now;
                written++;
                continue;
            }

            // Если позже уже есть период, новый блок закрывается днём до него — иначе открытый
            // интервал всегда пересекается с будущей записью (как у норм ИИС: from < to соседа).
            var insertedTo = ResolveInsertedEffectiveTo(forDir, effectiveFrom);
            ValidatePeriod(forDir, effectiveFrom, insertedTo, simulateAutoClose: true);
            AutoClosePrevious(forDir, effectiveFrom, now);

            var entity = new IicWorkDirectionWorkCost
            {
                WorkDirectionId = directionId,
                RatePerHour = rate,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = insertedTo,
                CreatedAt = now,
            };
            context.IicWorkDirectionWorkCosts.Add(entity);
            existing.Add(entity);
            written++;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new WorkCostBatchResult(written);
    }

    public async Task<WorkCostPeriodDeleteResult> DeletePeriodAsync(
        DateTime effectiveFrom,
        CancellationToken cancellationToken = default)
    {
        var from = DateOnlyUtc(effectiveFrom);
        var next = from.AddDays(1);
        var block = await context.IicWorkDirectionWorkCosts
            .Where(c => c.EffectiveFrom >= from && c.EffectiveFrom < next)
            .ToListAsync(cancellationToken);
        if (block.Count == 0)
            return new WorkCostPeriodDeleteResult(0);

        var directionIds = block.Select(c => c.WorkDirectionId).Distinct().ToList();
        var related = await context.IicWorkDirectionWorkCosts
            .Where(c => directionIds.Contains(c.WorkDirectionId))
            .ToListAsync(cancellationToken);

        var closeDate = from.AddDays(-1);
        foreach (var deleted in block)
        {
            var previous = related
                .Where(c => c.WorkDirectionId == deleted.WorkDirectionId
                    && c.Id != deleted.Id
                    && c.EffectiveFrom < from
                    && c.EffectiveTo != null
                    && c.EffectiveTo >= closeDate
                    && c.EffectiveTo < from)
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefault();
            if (previous is null)
                continue;

            previous.EffectiveTo = deleted.EffectiveTo;
            previous.ModifiedAt = DateTime.UtcNow;
        }

        context.IicWorkDirectionWorkCosts.RemoveRange(block);
        await context.SaveChangesAsync(cancellationToken);
        return new WorkCostPeriodDeleteResult(block.Count);
    }

    private static DateTime DateOnlyUtc(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static List<WorkCostRowDto> MapTree(
        IReadOnlyDictionary<int, IicWorkDirection> directionMap,
        IReadOnlyDictionary<int, IicWorkDirectionWorkCost> rates) =>
        directionMap.Values
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => Map(d, rates, directionMap))
            .ToList();

    private static void AutoClosePrevious(
        IReadOnlyList<IicWorkDirectionWorkCost> forDirection,
        DateTime newEffectiveFrom,
        DateTime now)
    {
        var closeDate = newEffectiveFrom.AddDays(-1);
        foreach (var row in forDirection)
        {
            if (row.EffectiveFrom >= newEffectiveFrom)
                continue;

            if (row.EffectiveTo is null || row.EffectiveTo >= newEffectiveFrom)
            {
                row.EffectiveTo = closeDate;
                row.ModifiedAt = now;
            }
        }
    }

    /// <summary>Конец нового периода: день до следующего «Действует с», иначе открытый.</summary>
    private static DateTime? ResolveInsertedEffectiveTo(
        IReadOnlyList<IicWorkDirectionWorkCost> existing,
        DateTime effectiveFrom)
    {
        var next = existing
            .Where(c => c.EffectiveFrom.Date > effectiveFrom.Date)
            .OrderBy(c => c.EffectiveFrom)
            .FirstOrDefault();
        if (next is null)
            return null;

        return DateTime.SpecifyKind(next.EffectiveFrom.Date.AddDays(-1), DateTimeKind.Utc);
    }

    private static void ValidatePeriod(
        IReadOnlyList<IicWorkDirectionWorkCost> existing,
        DateTime effectiveFrom,
        DateTime? newEffectiveTo,
        bool simulateAutoClose)
    {
        var from = effectiveFrom.Date;
        var closeDate = from.AddDays(-1);
        var newTo = newEffectiveTo?.Date ?? DateTime.MaxValue;

        foreach (var row in existing)
        {
            if (row.EffectiveFrom.Date == from)
                throw new InvalidOperationException(
                    "Ставка с такой датой «Действует с» уже существует для этого направления.");

            var rowTo = ResolveToForOverlap(row, from, closeDate, simulateAutoClose);
            if (from < rowTo && row.EffectiveFrom.Date < newTo)
            {
                throw new InvalidOperationException(
                    "Период действия пересекается с существующей записью для этого направления.");
            }
        }
    }

    private static DateTime ResolveToForOverlap(
        IicWorkDirectionWorkCost row,
        DateTime newFrom,
        DateTime closeDate,
        bool simulateAutoClose)
    {
        if (simulateAutoClose
            && row.EffectiveFrom < newFrom
            && (row.EffectiveTo is null || row.EffectiveTo >= newFrom))
        {
            return closeDate;
        }

        return row.EffectiveTo ?? DateTime.MaxValue;
    }

    private async Task<IReadOnlyDictionary<int, IicWorkDirection>> LoadDirectionMapAsync(
        CancellationToken cancellationToken) =>
        await context.IicWorkDirections.AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .ToDictionaryAsync(d => d.Id, cancellationToken);

    private static WorkCostRowDto Map(
        IicWorkDirection direction,
        IReadOnlyDictionary<int, IicWorkDirectionWorkCost> latestByDirection,
        IReadOnlyDictionary<int, IicWorkDirection> directionMap)
    {
        latestByDirection.TryGetValue(direction.Id, out var period);
        var name = IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(direction, directionMap);
        return new WorkCostRowDto(
            direction.Id,
            direction.Id,
            direction.ParentId,
            direction.SortOrder,
            name,
            direction.Code,
            period?.RatePerHour,
            period?.EffectiveFrom,
            period?.Id);
    }
}
