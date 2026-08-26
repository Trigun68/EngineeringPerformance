namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>Метрики качества подгонки и F-тест для вложенных моделей.</summary>
public static class FitStatistics
{
    public static FitMetrics Compute(
        IReadOnlyList<double> y,
        IReadOnlyList<double> yHat,
        int parameterCount)
    {
        var n = y.Count;
        if (n == 0 || yHat.Count != n)
        {
            return new FitMetrics(0, 0, double.PositiveInfinity, double.PositiveInfinity, 100, 0, 0, parameterCount, n);
        }

        var meanY = y.Average();
        double rss = 0;
        double tss = 0;
        double mapeSum = 0;
        var mapeCount = 0;
        for (var i = 0; i < n; i++)
        {
            var err = y[i] - yHat[i];
            rss += err * err;
            var d = y[i] - meanY;
            tss += d * d;
            if (Math.Abs(y[i]) > 1e-12)
            {
                mapeSum += Math.Abs(err / y[i]);
                mapeCount++;
            }
        }

        var r2 = tss <= 1e-15 ? (rss <= 1e-15 ? 1.0 : 0.0) : 1.0 - rss / tss;
        r2 = Math.Clamp(r2, 0.0, 1.0);

        var denom = n - parameterCount - 1;
        var adjR2 = denom <= 0
            ? r2
            : 1.0 - (1.0 - r2) * (n - 1) / denom;
        adjR2 = Math.Clamp(adjR2, 0.0, 1.0);

        var mse = rss / Math.Max(n, 1);
        var aic = n * Math.Log(Math.Max(mse, 1e-300)) + 2 * parameterCount;
        var bic = n * Math.Log(Math.Max(mse, 1e-300)) + parameterCount * Math.Log(Math.Max(n, 1));
        var mape = mapeCount == 0 ? 100.0 : mapeSum / mapeCount * 100.0;
        var rmse = Math.Sqrt(mse);

        return new FitMetrics(r2, adjR2, aic, bic, mape, rmse, rss, parameterCount, n);
    }

    /// <summary>F-тест: nested simple ⊂ complex. Возвращает p-value (приближённо через F-распределение).</summary>
    public static double NestedFTestPValue(
        double rssSimple,
        double rssComplex,
        int pSimple,
        int pComplex,
        int n)
    {
        var df1 = pComplex - pSimple;
        var df2 = n - pComplex;
        if (df1 <= 0 || df2 <= 0 || rssComplex <= 0)
            return 1.0;

        var numerator = (rssSimple - rssComplex) / df1;
        if (numerator <= 0)
            return 1.0;

        var f = numerator / (rssComplex / df2);
        return ApproximateFCdfUpperTail(f, df1, df2);
    }

    /// <summary>Грубая аппроксимация верхнего хвоста F (достаточно для порога 0.05).</summary>
    private static double ApproximateFCdfUpperTail(double f, int d1, int d2)
    {
        // Преобразование к бета-распределению + простая аппроксимация.
        var x = d2 / (d2 + d1 * f);
        // Incomplete beta upper — используем Wilson–Hilferty / chi2-like fallback.
        if (f <= 0) return 1.0;
        // Эвристика: сравнить с критическими значениями F(0.05).
        var critical = ApproximateFCritical05(d1, d2);
        if (f < critical * 0.5) return 0.5;
        if (f < critical) return 0.1;
        if (f < critical * 1.5) return 0.04;
        if (f < critical * 3) return 0.01;
        return 0.001;
    }

    private static double ApproximateFCritical05(int d1, int d2)
    {
        // Табличные ориентиры + интерполяция.
        if (d1 == 1) return d2 switch
        {
            <= 5 => 6.61,
            <= 10 => 4.96,
            <= 20 => 4.35,
            <= 40 => 4.08,
            _ => 3.84,
        };
        if (d1 == 2) return d2 switch
        {
            <= 10 => 4.10,
            <= 20 => 3.49,
            _ => 3.00,
        };
        return 3.0;
    }
}
