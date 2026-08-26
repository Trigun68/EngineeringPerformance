namespace EngineeringPerformance.Domain.Norms;

public enum FormulaType
{
    Linear = 1,
    Logarithmic = 2,
    ShiftedLog = 3,
    Wright = 4,
    Power = 5,
    ExponentialDecay = 6,
    PiecewiseLinear = 7,
    Polynomial = 8,
    /// <summary>N(V) = a — производительность не зависит от объёма.</summary>
    Constant = 9,
    /// <summary>N(V) = a + b/V — гиперболическая (асимптота).</summary>
    Hyperbolic = 10,
    /// <summary>N(V) = a · e^(b·V) — экспоненциальный рост/спад.</summary>
    Exponential = 11,
}
