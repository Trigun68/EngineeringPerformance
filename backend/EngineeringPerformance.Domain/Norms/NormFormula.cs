namespace EngineeringPerformance.Domain.Norms;

/// <summary>Параметрическая формула нормы N(V).</summary>
public sealed class NormFormula
{
    public FormulaType Type { get; set; } = FormulaType.Linear;

    public Dictionary<string, decimal> Coefficients { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Точки ломаной (PiecewiseLinear).</summary>
    public List<PiecewisePoint> PiecewisePoints { get; set; } = [];

    /// <summary>Коэффициенты полинома a0 + a1*V + ... (степень ≤ 5).</summary>
    public List<decimal> PolynomialCoeffs { get; set; } = [];

    public decimal MinVolume { get; set; }

    public decimal MaxVolume { get; set; } = 999_999m;

    public ExtrapolationMode Extrapolation { get; set; } = ExtrapolationMode.LinearFromEdge;
}
