namespace EngineeringPerformance.Domain.Norms;

public enum DeviationModelType
{
    Multiplicative = 1,
    Constant = 2,
    ProportionalToNorm = 3,
    ProportionalToSqrtV = 4,
    /// <summary>LI = e^(μ + z·σ) · V — логнормаль («Кривая Кима»).</summary>
    Lognormal = 5,
}
