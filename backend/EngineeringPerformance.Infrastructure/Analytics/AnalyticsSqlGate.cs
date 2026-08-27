namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>Не больше двух одновременных тяжёлых SQL-загрузок версии (page-data, экспорт, сравнение).</summary>
public sealed class AnalyticsSqlGate
{
    private readonly SemaphoreSlim _limit = new(2, 2);

    public Task WaitAsync(CancellationToken cancellationToken) => _limit.WaitAsync(cancellationToken);

    public void Release() => _limit.Release();
}
