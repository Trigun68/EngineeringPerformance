namespace EngineeringPerformance.Domain.Norms;

/// <summary>Единая точка расчёта N(V) для всех типов формул.</summary>
public static class NormFormulaEvaluator
{
    public static decimal Calculate(NormFormula formula, decimal volume)
    {
        var (v, isZero) = ResolveVolume(formula, volume);
        if (isZero) return 0m;
        return CalculateCore(formula, v);
    }

    private static (decimal Volume, bool IsZero) ResolveVolume(NormFormula formula, decimal volume)
    {
        if (formula.Type == FormulaType.Linear)
            return (volume, false);

        var min = formula.MinVolume;
        var max = formula.MaxVolume > 0 ? formula.MaxVolume : 999_999m;

        if (volume < min || volume > max)
        {
            if (formula.Extrapolation == ExtrapolationMode.Zero)
                return (0m, true);
            return (volume < min ? min : max, false);
        }

        return (volume, false);
    }

    private static decimal CalculateCore(NormFormula formula, decimal v)
    {
        var c = formula.Coefficients;
        return formula.Type switch
        {
            FormulaType.Linear => Math.Max(0m, Coeff(c, "a") - Coeff(c, "b") * v),
            FormulaType.Logarithmic when v < 1m => 0m,
            FormulaType.Logarithmic => Math.Max(0m, Coeff(c, "a") - Coeff(c, "b") * (decimal)Math.Log((double)v)),
            FormulaType.ShiftedLog => EvaluateShiftedLog(c, v),
            FormulaType.Wright when v <= 0m => 0m,
            FormulaType.Wright => Coeff(c, "a") * (decimal)Math.Pow((double)v, -(double)Coeff(c, "b")),
            FormulaType.Power when v <= 0m => 0m,
            FormulaType.Power => Coeff(c, "a") * (decimal)Math.Pow((double)v, (double)Coeff(c, "b")),
            FormulaType.ExponentialDecay => Coeff(c, "a") * (decimal)Math.Exp(-(double)Coeff(c, "b") * (double)v) + Coeff(c, "c"),
            FormulaType.PiecewiseLinear => EvaluatePiecewise(formula.PiecewisePoints, v),
            FormulaType.Polynomial => EvaluatePolynomial(formula.PolynomialCoeffs, v),
            FormulaType.Constant => Math.Max(0m, Coeff(c, "a")),
            FormulaType.Hyperbolic when v <= 0m => 0m,
            FormulaType.Hyperbolic => Math.Max(0m, Coeff(c, "a") + Coeff(c, "b") / v),
            FormulaType.Exponential => Math.Max(0m, Coeff(c, "a") * (decimal)Math.Exp((double)Coeff(c, "b") * (double)v)),
            _ => throw new ArgumentOutOfRangeException(nameof(formula.Type)),
        };
    }

    private static decimal EvaluateShiftedLog(IReadOnlyDictionary<string, decimal> c, decimal v)
    {
        var arg = (double)(v + Coeff(c, "c"));
        if (arg <= 0) return 0m;
        return Math.Max(0m, Coeff(c, "a") - Coeff(c, "b") * (decimal)Math.Log(arg));
    }

    internal static decimal EvaluatePiecewise(IReadOnlyList<PiecewisePoint>? points, decimal volume)
    {
        if (points is null || points.Count < 2)
            return 0m;

        var sorted = points.OrderBy(p => p.Volume).ToList();
        if (volume <= sorted[0].Volume)
            return Math.Max(0m, sorted[0].Norm);
        if (volume >= sorted[^1].Volume)
            return Math.Max(0m, sorted[^1].Norm);

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var p0 = sorted[i];
            var p1 = sorted[i + 1];
            if (volume >= p0.Volume && volume <= p1.Volume)
            {
                var t = (volume - p0.Volume) / (p1.Volume - p0.Volume);
                return Math.Max(0m, p0.Norm + t * (p1.Norm - p0.Norm));
            }
        }

        return 0m;
    }

    internal static decimal EvaluatePolynomial(IReadOnlyList<decimal>? coeffs, decimal volume)
    {
        if (coeffs is null || coeffs.Count == 0 || coeffs.Count > 6)
            return 0m;

        decimal sum = 0m;
        decimal power = 1m;
        foreach (var a in coeffs)
        {
            sum += a * power;
            power *= volume;
        }

        return Math.Max(0m, sum);
    }

    private static decimal Coeff(IReadOnlyDictionary<string, decimal> c, string name) =>
        c.TryGetValue(name, out var v) ? v : 0m;
}
