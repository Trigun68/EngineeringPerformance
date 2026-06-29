using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Core.Services;

public interface IDataTableService<T> where T : DataTableRecord
{
    Task<IReadOnlyList<T>> GetByProjectVersionAsync(int projectAnalysisVersionId);

    Task<T?> GetByIdAsync(int id);

    Task<T> CreateAsync(T item);

    Task UpdateAsync(T item);

    Task DeleteAsync(int id);
}
