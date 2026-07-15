using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public sealed class DeviationCalculator : IDeviationCalculator
{
    private readonly INormCalculator _normCalculator;

    public DeviationCalculator(INormCalculator normCalculator)
    {
        _normCalculator = normCalculator;
    }

    public decimal CalculateLaborIntensity(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        decimal zScore)
    {
        var n = _normCalculator.Calculate(normFormula, volume);
        return deviationFormula.ModelType switch
        {
            DeviationModelType.Multiplicative =>
                n * (1 + Coeff(deviationFormula, "sigma") * zScore) * effectiveVolume,
            DeviationModelType.Constant =>
                (n + Coeff(deviationFormula, "sigma")) * effectiveVolume,
            DeviationModelType.ProportionalToNorm =>
                n * (1 + Coeff(deviationFormula, "k") * zScore) * effectiveVolume,
            DeviationModelType.ProportionalToSqrtV =>
                (n + Coeff(deviationFormula, "sigma0") * (decimal)Math.Sqrt((double)Math.Max(0, volume)) * zScore) * effectiveVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(deviationFormula.ModelType)),
        };
    }

    public IReadOnlyList<NormBandPoint> GenerateBand(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        int points = 100)
    {
        points = Math.Max(2, points);
        var vMin = normFormula.Type == FormulaType.Linear ? 0.1m : (normFormula.MinVolume > 0 ? normFormula.MinVolume : 0.1m);
        var vMax = normFormula.MaxVolume > 0 ? normFormula.MaxVolume : 1000m;
        if (vMax <= vMin) vMax = vMin + 1m;

        var step = (vMax - vMin) / (points - 1);
        var result = new List<NormBandPoint>(points);

        for (var i = 0; i < points; i++)
        {
            var v = vMin + step * i;
            var n = _normCalculator.Calculate(normFormula, v);
            var (upper, lower) = ComputeBandBounds(n, v, deviationFormula);
            result.Add(new NormBandPoint
            {
                Volume = v,
                Center = n,
                Upper = Math.Max(0m, upper),
                Lower = Math.Max(0m, lower),
            });
        }

        return result;
    }

    internal static (decimal Upper, decimal Lower) ComputeBandBounds(
        decimal n,
        decimal volume,
        DeviationFormula deviationFormula) =>
        deviationFormula.ModelType switch
        {
            DeviationModelType.Multiplicative => (
                n * (1 + Coeff(deviationFormula, "sigma")),
                n * (1 - Coeff(deviationFormula, "sigma"))),
            DeviationModelType.Constant => (
                n + Coeff(deviationFormula, "sigma"),
                n - Coeff(deviationFormula, "sigma")),
            DeviationModelType.ProportionalToNorm => (
                n * (1 + Coeff(deviationFormula, "k")),
                n * (1 - Coeff(deviationFormula, "k"))),
            DeviationModelType.ProportionalToSqrtV => (
                n + Coeff(deviationFormula, "sigma0") * (decimal)Math.Sqrt((double)Math.Max(0, volume)),
                n - Coeff(deviationFormula, "sigma0") * (decimal)Math.Sqrt((double)Math.Max(0, volume))),
            _ => throw new ArgumentOutOfRangeException(nameof(deviationFormula.ModelType)),
        };

    private static decimal Coeff(DeviationFormula df, string name) =>
        df.Coefficients.TryGetValue(name, out var v) ? v : 0m;
}
