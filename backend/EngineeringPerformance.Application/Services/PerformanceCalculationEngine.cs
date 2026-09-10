using EngineeringPerformance.Application.Norms;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Вся математика агрегации ИИС, Z-score и распределения LI — без доступа к БД.
/// </summary>
public sealed partial class PerformanceCalculationEngine : IPerformanceCalculationEngine
{
    private readonly IDeviationCalculator _deviationCalculator;

    /// <summary>
    /// Оценка нормы/факта в лог-шкале: Estimate = ln(производительность), при 0 → 0.
    /// Конвенция хранения норм ИИС — единственный SoT перехода производительность → estimate.
    /// </summary>
    public static decimal ProductivityToEstimate(decimal productivity) =>
        productivity > 0 ? (decimal)Math.Log((double)productivity) : 0m;

    /// <summary>
    /// Обратный переход estimate → производительность: P = e^estimate.
    /// Единственный SoT; прямые <c>Math.Exp</c> над estimate вне движка запрещены.
    /// </summary>
    public static decimal EstimateToProductivity(decimal estimate) =>
        (decimal)Math.Exp((double)estimate);

    public PerformanceCalculationEngine(IDeviationCalculator deviationCalculator)
    {
        _deviationCalculator = deviationCalculator;
    }
}
