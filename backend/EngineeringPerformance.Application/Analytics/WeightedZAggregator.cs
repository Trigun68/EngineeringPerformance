namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Единственная реализация формулы взвешенного Z: Σ(Z×вес)/Σ(вес).
/// Internal-примитив движка расчётов: наружу (Infrastructure, билдеры, сервисы) доступен
/// только <c>IPerformanceCalculationEngine.ComputeWeightedZ</c> / <c>ComputeWeightedZFromPerformanceRows</c>.
/// Собственные реализации этой формулы вне движка запрещены (ISS-010).
/// </summary>
internal struct WeightedZAggregator
{
    private decimal _weightedSum;
    private decimal _weightSum;
    private decimal _zSum;
    private int _zCount;

    /// <summary>Сумма учтённых весов (только положительные).</summary>
    public readonly decimal WeightSum => _weightSum;

    /// <summary>Число значений Z в выборке, включая строки с нулевым весом.</summary>
    public readonly int SampleCount => _zCount;

    /// <summary>Строки без Z в выборку не попадают; вес учитывается только положительный.</summary>
    public void Add(decimal? zScore, decimal? weight)
    {
        if (zScore is not { } z)
            return;

        _zSum += z;
        _zCount++;

        if (weight is not { } w || w <= 0)
            return;

        _weightedSum += z * w;
        _weightSum += w;
    }

    /// <param name="arithmeticFallback">
    /// При нулевом суммарном весе вернуть среднее арифметическое Z вместо null.
    /// Нужен rollup Z параметров планирования (диалог и сводный отчёт).
    /// </param>
    public readonly decimal? Result(
        int decimalPlaces = 2,
        MidpointRounding rounding = MidpointRounding.ToEven,
        bool arithmeticFallback = false)
    {
        if (_weightSum > 0)
            return Math.Round(_weightedSum / _weightSum, decimalPlaces, rounding);

        if (arithmeticFallback && _zCount > 0)
            return Math.Round(_zSum / _zCount, decimalPlaces, rounding);

        return null;
    }
}
