namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Кэш тяжёлых аналитических агрегатов проекта (page-data, отчёт производительности).
/// Ключи версионируются поколением: любая мутация данных проекта делает старые записи
/// недостижимыми, поэтому отдавать устаревшие цифры невозможно (ISS-018).
/// </summary>
public interface IProjectAnalyticsCache
{
    /// <param name="key">Логический ключ внутри проекта, например <c>page-data:v12:c13</c>.</param>
    Task<T?> GetAsync<T>(int projectId, string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(int projectId, string key, T value, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Сбросить кэш одного проекта: импорт, правка данных, копирование/пересчёт версии.</summary>
    Task InvalidateProjectAsync(int projectId, CancellationToken cancellationToken = default);

    /// <summary>Сбросить кэш всех проектов: изменение норм ИИС влияет на любую версию.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Кэш портфельной аналитики (тренды Z/производительности, факт vs норма).
    /// Такие выборки зависят от данных многих проектов, поэтому версионируются
    /// портфельным поколением: оно поднимается при мутации ЛЮБОГО проекта.
    /// </summary>
    /// <param name="key">Логический ключ выборки (фильтры, пользователь, режим).</param>
    Task<T?> GetPortfolioAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetPortfolioAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        where T : class;
}
