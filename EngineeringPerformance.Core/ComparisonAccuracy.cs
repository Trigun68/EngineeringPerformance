namespace EngineeringPerformance.Core;

public static class ComparisonAccuracy
{
    public const string VeryHigh = "Очень высокая";
    public const string High = "Высокая";
    public const string Standard = "Стандартная";
    public const string Reduced = "Пониженная";
    public const string NotApplicable = "—";

    /// <summary>
    /// Точность сравнения по значению PD%:
    /// <list type="bullet">
    ///   <item>&lt; 7% — Очень высокая</item>
    ///   <item>7% … &lt; 9% — Высокая</item>
    ///   <item>9% … &lt; 11% — Стандартная</item>
    ///   <item>&gt;= 11% — Пониженная</item>
    /// </list>
    /// </summary>
    public static string FromValuePercent(decimal? valuePercent)
    {
        if (valuePercent is null) return NotApplicable;
        var v = valuePercent.Value;
        if (v < 7m) return VeryHigh;
        if (v < 9m) return High;
        if (v < 11m) return Standard;
        return Reduced;
    }
}
