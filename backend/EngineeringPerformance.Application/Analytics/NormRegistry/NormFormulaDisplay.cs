using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics.NormRegistry;

public static class NormFormulaDisplay
{
    public static string Format(FormulaType type, IReadOnlyDictionary<string, decimal> coefficients)
    {
        string C(string key, string fallback = "…")
        {
            if (coefficients.TryGetValue(key, out var v)) return FormatNum(v);
            var m = coefficients.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(m.Key) ? fallback : FormatNum(m.Value);
        }

        return type switch
        {
            FormulaType.Linear => $"y = {C("a")} + {C("b")}·x",
            FormulaType.Constant => $"y = {C("a")}",
            FormulaType.Power => $"y = {C("a")} · x^{C("b")}",
            FormulaType.Logarithmic => $"y = {C("a")} + {C("b")}·ln(x)",
            FormulaType.Exponential => $"y = {C("a")} · e^({C("b")}·x)",
            FormulaType.ExponentialDecay => $"y = {C("a")} · e^(-{C("b")}·x)",
            FormulaType.Hyperbolic => $"y = {C("a")} + {C("b")}/x",
            FormulaType.Wright => $"Wright({C("a")}, {C("b")})",
            FormulaType.ShiftedLog => $"y = {C("a")} + {C("b")}·ln(x + {C("c", "0")})",
            FormulaType.Polynomial => "Полином",
            FormulaType.PiecewiseLinear => "Ломаная",
            FormulaType.KimCurve => $"P(z) = e^({C("mu")} + z·{C("sigma")})",
            _ => type.ToString(),
        };
    }

    private static string FormatNum(decimal v) =>
        v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
}
