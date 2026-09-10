namespace EngineeringPerformance.Domain.Norms;

public enum DeviationModelType
{
    Multiplicative = 1,
    Constant = 2,
    ProportionalToNorm = 3,
    ProportionalToSqrtV = 4,
    /// <summary>LI = e^(μ + z·σ) · V — логнормальное распределение.</summary>
    Lognormal = 5,
}
