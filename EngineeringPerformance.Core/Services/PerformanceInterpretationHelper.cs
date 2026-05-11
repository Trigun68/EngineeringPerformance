using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.Services;

// Маппинг Z-score в категорию интерпретации производительности.
// Логика идентична PerformanceService.DetermineInterpretation (исторический источник до выноса в helper).
// Пороги: Z < -1 → Excellent, -1 ≤ Z < -0.5 → Good, -0.5 ≤ Z ≤ 0.5 → Satisfactory,
// 0.5 < Z ≤ 1 → NeedsAttention, Z > 1 → Critical.
// Меньший Z = меньше часов на единицу = выше производительность.
public static class PerformanceInterpretationHelper
{
    public static PerformanceInterpretation? GetInterpretation(decimal? z)
    {
        if (!z.HasValue) return null;
        var v = z.Value;
        if (v < -1.0m) return PerformanceInterpretation.Excellent;
        if (v < -0.5m) return PerformanceInterpretation.Good;
        if (v <= 0.5m) return PerformanceInterpretation.Satisfactory;
        if (v <= 1.0m) return PerformanceInterpretation.NeedsAttention;
        return PerformanceInterpretation.Critical;
    }
}
