using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>Маппинг референтных кодов ↔ FormulaType регистра норм.</summary>
public static class FormulaTypeMapper
{
    public static string ToReferenceCode(FormulaType type) => type switch
    {
        FormulaType.Constant => ReferenceFormulaCodes.Constant,
        FormulaType.Linear => ReferenceFormulaCodes.Linear,
        FormulaType.Power => ReferenceFormulaCodes.Power,
        FormulaType.Wright => ReferenceFormulaCodes.Power,
        FormulaType.Logarithmic => ReferenceFormulaCodes.Logarithmic,
        FormulaType.ShiftedLog => ReferenceFormulaCodes.Logarithmic,
        FormulaType.Exponential => ReferenceFormulaCodes.Exponential,
        FormulaType.ExponentialDecay => ReferenceFormulaCodes.Exponential,
        FormulaType.Polynomial => ReferenceFormulaCodes.Polynomial2,
        FormulaType.Hyperbolic => ReferenceFormulaCodes.Hyperbolic,
        _ => ReferenceFormulaCodes.Linear,
    };

    public static FormulaType ToFormulaType(string referenceCode) =>
        referenceCode.ToUpperInvariant() switch
        {
            ReferenceFormulaCodes.Constant => FormulaType.Constant,
            ReferenceFormulaCodes.Linear => FormulaType.Linear,
            ReferenceFormulaCodes.Power => FormulaType.Power,
            ReferenceFormulaCodes.Logarithmic => FormulaType.Logarithmic,
            ReferenceFormulaCodes.Exponential => FormulaType.Exponential,
            ReferenceFormulaCodes.Polynomial2 => FormulaType.Polynomial,
            ReferenceFormulaCodes.Hyperbolic => FormulaType.Hyperbolic,
            _ => FormulaType.Linear,
        };

    /// <summary>
    /// Параметры референтной LINEAR (y=a+b·x) → EPMS Linear (N=max(0,a'-b'·V)).
    /// Если b≥0 (рост), сохраняем как Polynomial степени 1.
    /// </summary>
    public static (FormulaType Type, Dictionary<string, double> Coefficients, double[]? PolynomialCoeffs)
        ToNormStorage(string referenceCode, IReadOnlyDictionary<string, double> parameters)
    {
        var code = referenceCode.ToUpperInvariant();
        var dict = parameters.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        if (code == ReferenceFormulaCodes.Linear)
        {
            var a = dict.GetValueOrDefault("a");
            var b = dict.GetValueOrDefault("b");
            if (b <= 0)
            {
                return (FormulaType.Linear, new Dictionary<string, double> { ["a"] = a, ["b"] = Math.Abs(b) }, null);
            }

            return (FormulaType.Polynomial, new Dictionary<string, double>(), [a, b]);
        }

        if (code == ReferenceFormulaCodes.Polynomial2)
        {
            return (
                FormulaType.Polynomial,
                new Dictionary<string, double>(),
                [dict.GetValueOrDefault("a"), dict.GetValueOrDefault("b"), dict.GetValueOrDefault("c")]);
        }

        if (code == ReferenceFormulaCodes.Logarithmic)
        {
            // Референс: a + b·ln(x); EPMS: max(0, a - b·ln(x)) → b_epms = -b_ref.
            var a = dict.GetValueOrDefault("a");
            var b = dict.GetValueOrDefault("b");
            return (FormulaType.Logarithmic, new Dictionary<string, double> { ["a"] = a, ["b"] = -b }, null);
        }

        return (ToFormulaType(code), dict, null);
    }
}
