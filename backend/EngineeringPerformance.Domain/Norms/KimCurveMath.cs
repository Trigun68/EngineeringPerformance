namespace EngineeringPerformance.Domain.Norms;

/// <summary>
/// Логнормальное распределение: μ = ln(P), P(z) = e^(μ + z·σ).
/// </summary>
public static class KimCurveMath
{
    public static decimal Ln(decimal value) =>
        value > 0 ? (decimal)Math.Log((double)value) : 0m;

    public static decimal Exp(decimal value)
    {
        var x = Math.Exp((double)value);
        if (double.IsNaN(x) || double.IsInfinity(x) || x > (double)decimal.MaxValue)
            return 0m;
        return (decimal)x;
    }

    public static decimal ProductivityAtZ(decimal mu, decimal sigma, decimal zScore) =>
        Exp(mu + zScore * sigma);
}
