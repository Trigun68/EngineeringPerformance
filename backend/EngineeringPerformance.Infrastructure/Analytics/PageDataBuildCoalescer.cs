using System.Collections.Concurrent;
using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Один cold-build page-data на ключ версии: summary и detail ждут один Task.
/// Сборка не отменяется по HTTP abort — результат пишется в кэш для следующего GET.
/// </summary>
public interface IPageDataBuildCoalescer
{
    Task<ProjectsPageDataEncodedDto> RunAsync(
        string key,
        Func<Task<ProjectsPageDataEncodedDto>> build,
        CancellationToken cancellationToken);
}

public sealed class PageDataBuildCoalescer : IPageDataBuildCoalescer
{
    private readonly ConcurrentDictionary<string, Lazy<Task<ProjectsPageDataEncodedDto>>> _inflight =
        new(StringComparer.Ordinal);

    public async Task<ProjectsPageDataEncodedDto> RunAsync(
        string key,
        Func<Task<ProjectsPageDataEncodedDto>> build,
        CancellationToken cancellationToken)
    {
        var lazy = _inflight.GetOrAdd(
            key,
            static (_, factory) => new Lazy<Task<ProjectsPageDataEncodedDto>>(
                factory,
                LazyThreadSafetyMode.ExecutionAndPublication),
            build);

        try
        {
            // Не WaitAsync(ct): abort клиента не должен рвать общий build и dispose scoped-сервисов.
            // Кэш всё равно пишется; следующий GET попадёт в hit.
            _ = cancellationToken;
            return await lazy.Value.ConfigureAwait(false);
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                _inflight.TryRemove(KeyValuePair.Create(key, lazy));
            }
        }
    }
}
