using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Маппинг Z-score в категорию интерпретации (пороги из legacy PerformanceService).
/// </summary>
public static class PerformanceInterpretationMapper
{
    public static Domain.Enums.PerformanceInterpretation? FromZScore(decimal? z)
    {
        if (!z.HasValue) return null;
        var v = z.Value;
        if (v < -1.0m) return Domain.Enums.PerformanceInterpretation.Excellent;
        if (v < -0.5m) return Domain.Enums.PerformanceInterpretation.Good;
        if (v <= 0.5m) return Domain.Enums.PerformanceInterpretation.Satisfactory;
        if (v <= 1.0m) return Domain.Enums.PerformanceInterpretation.NeedsAttention;
        return Domain.Enums.PerformanceInterpretation.Critical;
    }
}
