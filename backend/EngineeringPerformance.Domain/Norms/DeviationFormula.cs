namespace EngineeringPerformance.Domain.Norms;

/// <summary>Модель отклонения трудоёмкости от базовой нормы N(V).</summary>
public sealed class DeviationFormula
{
    public DeviationModelType ModelType { get; set; } = DeviationModelType.Multiplicative;

    public Dictionary<string, decimal> Coefficients { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
