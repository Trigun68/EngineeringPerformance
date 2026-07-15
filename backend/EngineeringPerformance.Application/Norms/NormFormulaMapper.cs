using System.Text.Json;
using System.Text.Json.Serialization;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

/// <summary>Сериализация NormFormula / DeviationFormula в JSON колонки БД.</summary>
public static class NormFormulaMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static NormFormula ToNormFormula(
        FormulaType formulaType,
        string? coefficientsJson,
        decimal minVolume,
        decimal maxVolume,
        ExtrapolationMode extrapolation,
        decimal? legacyPerformance = null)
    {
        var formula = new NormFormula
        {
            Type = formulaType,
            MinVolume = minVolume,
            MaxVolume = maxVolume,
            Extrapolation = extrapolation,
        };

        if (!string.IsNullOrWhiteSpace(coefficientsJson))
        {
            var doc = JsonDocument.Parse(coefficientsJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.NameEquals("piecewisePoints") && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    formula.PiecewisePoints = prop.Value.EnumerateArray()
                        .Select(e => new PiecewisePoint
                        {
                            Volume = e.GetProperty("volume").GetDecimal(),
                            Norm = e.GetProperty("norm").GetDecimal(),
                        })
                        .ToList();
                }
                else if (prop.NameEquals("polynomialCoeffs") && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    formula.PolynomialCoeffs = prop.Value.EnumerateArray().Select(e => e.GetDecimal()).ToList();
                }
                else if (prop.Value.ValueKind is JsonValueKind.Number)
                {
                    formula.Coefficients[prop.Name] = prop.Value.GetDecimal();
                }
            }
        }
        else if (legacyPerformance.HasValue)
        {
            formula.Coefficients["a"] = legacyPerformance.Value;
            formula.Coefficients["b"] = 0m;
        }

        if (formula.Coefficients.Count == 0 && legacyPerformance.HasValue)
        {
            formula.Coefficients["a"] = legacyPerformance.Value;
            formula.Coefficients["b"] = 0m;
        }

        return formula;
    }

    public static DeviationFormula ToDeviationFormula(
        DeviationModelType modelType,
        string? deviationCoefficientsJson,
        decimal? legacyPerformance = null,
        decimal? legacyStdDev = null)
    {
        var dev = new DeviationFormula { ModelType = modelType };

        if (!string.IsNullOrWhiteSpace(deviationCoefficientsJson))
        {
            var doc = JsonDocument.Parse(deviationCoefficientsJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind is JsonValueKind.Number)
                    dev.Coefficients[prop.Name] = prop.Value.GetDecimal();
            }
        }
        else if (legacyPerformance is > 0 && legacyStdDev.HasValue)
        {
            dev.ModelType = DeviationModelType.Multiplicative;
            dev.Coefficients["sigma"] = legacyStdDev.Value / legacyPerformance.Value;
        }

        if (dev.Coefficients.Count == 0 && legacyPerformance is > 0 && legacyStdDev.HasValue)
        {
            dev.ModelType = DeviationModelType.Multiplicative;
            dev.Coefficients["sigma"] = legacyStdDev.Value / legacyPerformance.Value;
        }
        else if (dev.ModelType == 0)
        {
            dev.ModelType = DeviationModelType.Multiplicative;
        }

        return dev;
    }

    public static string SerializeCoefficients(NormFormula formula)
    {
        var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in formula.Coefficients)
            obj[k] = v;
        if (formula.PiecewisePoints.Count > 0)
            obj["piecewisePoints"] = formula.PiecewisePoints.Select(p => new { volume = p.Volume, norm = p.Norm });
        if (formula.PolynomialCoeffs.Count > 0)
            obj["polynomialCoeffs"] = formula.PolynomialCoeffs;
        return JsonSerializer.Serialize(obj, JsonOptions);
    }

    public static string SerializeDeviationCoefficients(DeviationFormula formula) =>
        JsonSerializer.Serialize(formula.Coefficients, JsonOptions);

    public static NormFormula FromLegacyPerformance(decimal? performance) => new()
    {
        Type = FormulaType.Linear,
        Coefficients = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = performance ?? 0m,
            ["b"] = 0m,
        },
    };

    public static DeviationFormula FromLegacyStdDev(decimal? performance, decimal? stdDev)
    {
        var sigma = performance is > 0 && stdDev.HasValue ? stdDev.Value / performance.Value : 0m;
        return new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["sigma"] = sigma },
        };
    }
}
