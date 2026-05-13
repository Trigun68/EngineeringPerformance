using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Обёртка над <see cref="DirectoryCache{T}"/> для направлений ИИС: любая мутация кеша
/// сбрасывает процессный снэпшот дерева в <see cref="IPerformanceService"/>, иначе расчёт
/// производительности держит устаревшую иерархию до перезапуска приложения.
/// </summary>
public sealed class IicWorkDirectionDirectoryCacheDecorator : IDirectoryCache<IicWorkDirection>
{
    private readonly DirectoryCache<IicWorkDirection> _inner;
    private readonly IPerformanceService _performanceService;

    public IicWorkDirectionDirectoryCacheDecorator(
        DirectoryCache<IicWorkDirection> inner,
        IPerformanceService performanceService)
    {
        _inner = inner;
        _performanceService = performanceService;
    }

    public Task<IReadOnlyList<IicWorkDirection>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _inner.GetAllAsync(cancellationToken);

    public void Invalidate()
    {
        _inner.Invalidate();
        _performanceService.InvalidateWorkDirectionHierarchyCache();
    }

    public void Upsert(IicWorkDirection item)
    {
        _inner.Upsert(item);
        _performanceService.InvalidateWorkDirectionHierarchyCache();
    }

    public void Remove(int id)
    {
        _inner.Remove(id);
        _performanceService.InvalidateWorkDirectionHierarchyCache();
    }
}
