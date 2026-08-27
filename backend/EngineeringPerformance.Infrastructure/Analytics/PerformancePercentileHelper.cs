namespace EngineeringPerformance.Infrastructure.Analytics;

// Маппинг PEPM/Z-score в текстовый перцентильный диапазон по таблице из «Справки».
// Логика идентична ProjectAnalysisService.GetPercentile (исторический источник до выноса в helper):
// диапазон [-1, 1] линейно отображается на перцентили [0, 100], 10 равных корзин шагом 0.2,
// значения вне [-1, 1] клэмпятся к границам. Меньший Z = более высокая производительность
// (меньше часов на единицу), поэтому шкала перцентилей идёт в ту же сторону, что и Z.
public static class PerformancePercentileHelper
{
    public static string GetPercentile(decimal? z)
    {
        if (!z.HasValue) return "—";
        var v = z.Value;
        if (v < -1.0m) return "0-й";
        if (v < -0.8m) return "0-10-й";
        if (v < -0.6m) return "10-20-й";
        if (v < -0.4m) return "20-30-й";
        if (v < -0.2m) return "30-40-й";
        if (v < 0m)    return "40-50-й";
        if (v < 0.2m)  return "50-60-й";
        if (v < 0.4m)  return "60-70-й";
        if (v < 0.6m)  return "70-80-й";
        if (v < 0.8m)  return "80-90-й";
        if (v <= 1.0m) return "90-100-й";
        return "100-й";
    }
}
