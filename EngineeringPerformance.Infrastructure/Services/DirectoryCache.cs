using System.Diagnostics;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using Serilog;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Потоко-безопасная реализация <see cref="IDirectoryCache{T}"/> поверх
/// <see cref="IDirectoryService{T}"/>. Загружает все активные записи один раз
/// и отдаёт ссылку на тот же список при повторных вызовах. Точечные изменения
/// применяются copy-on-write — читатели получают консистентный снэпшот.
/// </summary>
public sealed class DirectoryCache<T> : IDirectoryCache<T> where T : DirectoryItem
{
    private readonly IDirectoryService<T> _service;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private volatile IReadOnlyList<T>? _cache;

    public DirectoryCache(IDirectoryService<T> service)
    {
        _service = service;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is { } cached)
        {
            Log.Information("[PERF] DirectoryCache<{Type}>.GetAllAsync HIT rows={Rows}", typeof(T).Name, cached.Count);
            return cached;
        }

        var swWait = Stopwatch.StartNew();
        await _gate.WaitAsync(cancellationToken);
        swWait.Stop();
        try
        {
            if (_cache is { } afterWait)
            {
                Log.Information("[PERF] DirectoryCache<{Type}>.GetAllAsync HIT-after-wait wait={Wait}ms rows={Rows}",
                    typeof(T).Name, swWait.ElapsedMilliseconds, afterWait.Count);
                return afterWait;
            }

            var swLoad = Stopwatch.StartNew();
            _cache = await _service.GetAllAsync(false);
            swLoad.Stop();
            Log.Information("[PERF] DirectoryCache<{Type}>.GetAllAsync MISS wait={Wait}ms load={Load}ms rows={Rows}",
                typeof(T).Name, swWait.ElapsedMilliseconds, swLoad.ElapsedMilliseconds, _cache.Count);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _cache = null;

    public void Upsert(T item)
    {
        var snapshot = _cache;
        if (snapshot is null) return;

        var next = new List<T>(snapshot.Count + 1);
        var replaced = false;
        foreach (var existing in snapshot)
        {
            if (existing.Id == item.Id)
            {
                next.Add(item);
                replaced = true;
            }
            else
            {
                next.Add(existing);
            }
        }

        if (!replaced) next.Add(item);

        _cache = next;
    }

    public void Remove(int id)
    {
        var snapshot = _cache;
        if (snapshot is null) return;

        var next = new List<T>(snapshot.Count);
        foreach (var existing in snapshot)
        {
            if (existing.Id != id) next.Add(existing);
        }

        _cache = next;
    }
}
