namespace EngineeringPerformance.Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
