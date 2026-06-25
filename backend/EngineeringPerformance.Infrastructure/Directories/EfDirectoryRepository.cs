using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Directories;

/// <summary>
/// Generic EF repository для справочников DirectoryItem (порт WPF DirectoryServiceBase).
/// </summary>
internal sealed class EfDirectoryRepository<T> where T : DirectoryItem, new()
{
    private readonly AppDbContext _context;
    private readonly Func<AppDbContext, DbSet<T>> _dbSet;

    public EfDirectoryRepository(AppDbContext context, Func<AppDbContext, DbSet<T>> dbSet)
    {
        _context = context;
        _dbSet = dbSet;
    }

    public Task<List<T>> GetAllAsync(bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbSet(_context).AsNoTracking();
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        return query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToListAsync(cancellationToken);
    }

    public Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        _dbSet(_context).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<T>> GetChildrenAsync(int? parentId, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = _dbSet(_context).AsNoTracking().Where(e => e.ParentId == parentId);
        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        return query.OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToListAsync(cancellationToken);
    }

    public async Task<T> CreateAsync(T item, CancellationToken cancellationToken)
    {
        item.CreatedAt = DateTime.UtcNow;
        _dbSet(_context).Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<T?> UpdateAsync(T item, CancellationToken cancellationToken)
    {
        var tracked = await _dbSet(_context).FirstOrDefaultAsync(e => e.Id == item.Id, cancellationToken);
        if (tracked is null)
            return null;

        item.ModifiedAt = DateTime.UtcNow;
        _context.Entry(tracked).CurrentValues.SetValues(item);
        await _context.SaveChangesAsync(cancellationToken);
        return tracked;
    }

    public async Task<bool> ArchiveAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet(_context).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return false;

        entity.IsArchived = true;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnarchiveAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet(_context).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return false;

        entity.IsArchived = false;
        entity.ModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbSet(_context).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
            return false;

        _dbSet(_context).Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateSortOrderAsync(int itemId, int? newParentId, int newSortOrder, CancellationToken cancellationToken)
    {
        var entity = await _dbSet(_context).FirstOrDefaultAsync(e => e.Id == itemId, cancellationToken);
        if (entity is null)
            return false;

        var oldParentId = entity.ParentId;
        var parentChanged = oldParentId != newParentId;

        entity.ParentId = newParentId;
        entity.SortOrder = newSortOrder;
        entity.ModifiedAt = DateTime.UtcNow;

        var newSiblings = await _dbSet(_context)
            .Where(e => e.Id != itemId && e.ParentId == newParentId)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken);

        var insertIndex = 0;
        foreach (var sibling in newSiblings)
        {
            if (insertIndex == newSortOrder)
                insertIndex++;
            sibling.SortOrder = insertIndex;
            insertIndex++;
        }

        if (parentChanged)
        {
            var oldSiblings = await _dbSet(_context)
                .Where(e => e.Id != itemId && e.ParentId == oldParentId)
                .OrderBy(e => e.SortOrder)
                .ToListAsync(cancellationToken);

            for (var i = 0; i < oldSiblings.Count; i++)
                oldSiblings[i].SortOrder = i;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
