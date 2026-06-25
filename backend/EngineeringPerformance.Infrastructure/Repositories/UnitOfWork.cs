using EngineeringPerformance.Application.Common.Interfaces;
using EngineeringPerformance.Infrastructure.Persistence;

namespace EngineeringPerformance.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}
