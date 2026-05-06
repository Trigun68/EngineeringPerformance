namespace EngineeringPerformance.Core.Enums;

public enum PerformanceInterpretation
{
    Excellent,
    Good,
    Satisfactory,
    NeedsAttention,
    Critical
}

public static class PerformanceInterpretationExtensions
{
    public static string ToRussian(this PerformanceInterpretation interpretation) => interpretation switch
    {
        PerformanceInterpretation.Excellent => "Отличная производительность",
        PerformanceInterpretation.Good => "Хорошая производительность",
        PerformanceInterpretation.Satisfactory => "Удовлетворительная производительность",
        PerformanceInterpretation.NeedsAttention => "Требует внимания",
        PerformanceInterpretation.Critical => "Критически низкая производительность",
        _ => interpretation.ToString(),
    };
}
