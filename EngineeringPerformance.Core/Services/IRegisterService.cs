using EngineeringPerformance.Core.Models;

namespace EngineeringPerformance.Core.Services;

public interface IRegisterService<T> where T : RegisterEntry
{
    Task<IReadOnlyList<T>> GetAllAsync();

    Task<T?> GetByIdAsync(int id);

    Task<T> CreateAsync(T item);

    Task UpdateAsync(T item);

    Task DeleteAsync(int id);
}

public interface IDateFilteredRegisterService<T> : IRegisterService<T> where T : RegisterEntry
{
    Task<IReadOnlyList<T>> GetByDateAsync(DateTime date);
}
