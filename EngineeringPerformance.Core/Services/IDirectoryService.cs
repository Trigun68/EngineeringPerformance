using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Core.Services;

public interface IDirectoryService<T> where T : DirectoryItem
{
    Task<IReadOnlyList<T>> GetAllAsync(bool includeArchived = false);

    Task<T?> GetByIdAsync(int id);

    Task<IReadOnlyList<T>> GetChildrenAsync(int? parentId = null, bool includeArchived = false);

    Task<T> CreateAsync(T item);

    Task UpdateAsync(T item);

    Task ArchiveAsync(int id);

    Task UnarchiveAsync(int id);

    Task DeleteAsync(int id);

    Task UpdateSortOrderAsync(int itemId, int? newParentId, int newSortOrder);
}
