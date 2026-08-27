namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>
/// Поколение кэша анализа «факт vs норма». Вынесено из <see cref="IFactNormAnalysisService"/>,
/// потому что справочнику норм нужен сброс кэша, а прямая зависимость от сервиса анализа
/// замкнула бы DI (сервис анализа сам зависит от справочника норм).
/// </summary>
public interface IFactNormCacheGeneration
{
    /// <summary>Текущее поколение; входит в ключ кэша, поэтому инкремент обесценивает записи.</summary>
    Task<long> GetAsync(CancellationToken cancellationToken = default);

    Task BumpAsync(CancellationToken cancellationToken = default);
}
