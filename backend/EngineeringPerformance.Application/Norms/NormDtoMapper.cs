using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public static class NormDtoMapper
{
    public static NormFormulaDto ToDto(NormFormula formula) => new(
        formula.Type,
        formula.Coefficients,
        formula.MinVolume,
        formula.MaxVolume,
        formula.Extrapolation,
        formula.PiecewisePoints.Count > 0
            ? formula.PiecewisePoints.Select(p => new PiecewisePointDto(p.Volume, p.Norm)).ToList()
            : null,
        formula.PolynomialCoeffs.Count > 0 ? formula.PolynomialCoeffs : null);

    public static DeviationFormulaDto ToDto(DeviationFormula formula) => new(
        formula.ModelType,
        formula.Coefficients);

    public static NormFormula ToEntity(NormFormulaDto? dto, decimal? legacyPerformance = null)
    {
        if (dto is null)
            return NormFormulaMapper.FromLegacyPerformance(legacyPerformance);

        var formula = new NormFormula
        {
            Type = dto.Type,
            MinVolume = dto.MinVolume,
            MaxVolume = dto.MaxVolume,
            Extrapolation = dto.Extrapolation,
            Coefficients = dto.Coefficients.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
        };

        if (dto.PiecewisePoints is not null)
            formula.PiecewisePoints = dto.PiecewisePoints.Select(p => new PiecewisePoint { Volume = p.Volume, Norm = p.Norm }).ToList();
        if (dto.PolynomialCoeffs is not null)
            formula.PolynomialCoeffs = dto.PolynomialCoeffs.ToList();

        return formula;
    }

    public static DeviationFormula ToEntity(DeviationFormulaDto? dto, decimal? legacyPerformance = null, decimal? legacyStdDev = null)
    {
        if (dto is null)
            return NormFormulaMapper.FromLegacyStdDev(legacyPerformance, legacyStdDev);

        return new DeviationFormula
        {
            ModelType = dto.ModelType,
            Coefficients = dto.Coefficients.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
        };
    }

    public static void ApplyToEntity(IicWorkDirectionNorm entity, IicNormWriteRequest request)
    {
        entity.Performance = request.Performance;
        entity.Estimate = request.Estimate;
        entity.StandardDeviation = request.StandardDeviation;

        var normFormula = ToEntity(request.NormFormula, request.Performance);
        var devFormula = ToEntity(request.DeviationFormula, request.Performance, request.StandardDeviation);

        if (normFormula.Type == FormulaType.KimCurve)
        {
            ApplyKimCurve(entity, request, normFormula, devFormula);
            return;
        }

        entity.FormulaType = normFormula.Type;
        entity.CoefficientsJson = NormFormulaMapper.SerializeCoefficients(normFormula);
        entity.MinVolume = normFormula.MinVolume;
        entity.MaxVolume = normFormula.MaxVolume;
        entity.ExtrapolationMode = normFormula.Extrapolation;
        entity.DeviationModelType = devFormula.ModelType;
        entity.DeviationCoefficientsJson = NormFormulaMapper.SerializeDeviationCoefficients(devFormula);

        entity.Performance = normFormula.Coefficients.GetValueOrDefault("a");
        entity.Estimate = request.Estimate;
        if (devFormula.ModelType == DeviationModelType.Multiplicative
            && devFormula.Coefficients.TryGetValue("sigma", out var sigma)
            && entity.Performance is > 0)
        {
            entity.StandardDeviation = sigma * entity.Performance;
        }
    }

    /// <summary>
    /// Кривая Кима: P, μ=ln(P), σ в лог-пространстве. Не пересчитывает линейное СКО как σ·P.
    /// </summary>
    private static void ApplyKimCurve(
        IicWorkDirectionNorm entity,
        IicNormWriteRequest request,
        NormFormula normFormula,
        DeviationFormula devFormula)
    {
        var performance = normFormula.Coefficients.GetValueOrDefault("a");
        if (performance <= 0 && request.Performance is > 0)
            performance = request.Performance.Value;
        if (performance <= 0)
            throw new InvalidOperationException("Кривая Кима: производительность должна быть больше 0.");

        var sigma = ResolveKimSigma(normFormula, devFormula, request.StandardDeviation);
        if (sigma <= 0)
            throw new InvalidOperationException("Кривая Кима: σ должна быть больше 0.");

        var mu = KimCurveMath.Ln(performance);
        normFormula.Coefficients["a"] = performance;
        normFormula.Coefficients["mu"] = mu;
        normFormula.Coefficients["sigma"] = sigma;

        var lognormal = new DeviationFormula
        {
            ModelType = DeviationModelType.Lognormal,
            Coefficients = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["sigma"] = sigma },
        };

        entity.FormulaType = FormulaType.KimCurve;
        entity.CoefficientsJson = NormFormulaMapper.SerializeCoefficients(normFormula);
        entity.MinVolume = normFormula.MinVolume;
        entity.MaxVolume = normFormula.MaxVolume > 0 ? normFormula.MaxVolume : 999_999m;
        entity.ExtrapolationMode = normFormula.Extrapolation;
        entity.DeviationModelType = DeviationModelType.Lognormal;
        entity.DeviationCoefficientsJson = NormFormulaMapper.SerializeDeviationCoefficients(lognormal);
        entity.Performance = performance;
        entity.Estimate = mu;
        entity.StandardDeviation = sigma;
    }

    private static decimal ResolveKimSigma(
        NormFormula normFormula,
        DeviationFormula devFormula,
        decimal? requestStdDev)
    {
        if (normFormula.Coefficients.TryGetValue("sigma", out var fromNorm) && fromNorm > 0)
            return fromNorm;
        if (devFormula.ModelType == DeviationModelType.Lognormal
            && devFormula.Coefficients.TryGetValue("sigma", out var fromDev)
            && fromDev > 0)
            return fromDev;
        if (requestStdDev is > 0)
            return requestStdDev.Value;
        if (devFormula.Coefficients.TryGetValue("sigma", out var fallback) && fallback > 0)
            return fallback;
        return 0m;
    }

    public static NormFormulaDto ToNormFormulaDto(IicWorkDirectionNorm entity)
    {
        var formula = NormFormulaMapper.ToNormFormula(
            entity.FormulaType,
            entity.CoefficientsJson,
            entity.MinVolume,
            entity.MaxVolume,
            entity.ExtrapolationMode,
            entity.Performance);
        if (formula.Type == FormulaType.KimCurve)
            HydrateKimCoefficients(formula, entity);
        return ToDto(formula);
    }

    private static void HydrateKimCoefficients(NormFormula formula, IicWorkDirectionNorm entity)
    {
        var performance = formula.Coefficients.GetValueOrDefault("a");
        if (performance <= 0 && entity.Performance is > 0)
            performance = entity.Performance.Value;
        formula.Coefficients["a"] = performance;
        formula.Coefficients["mu"] = entity.Estimate ?? KimCurveMath.Ln(performance);
        if (!formula.Coefficients.TryGetValue("sigma", out var sigma) || sigma <= 0)
            formula.Coefficients["sigma"] = entity.StandardDeviation ?? 0m;
    }

    public static DeviationFormulaDto ToDeviationFormulaDto(IicWorkDirectionNorm entity)
    {
        var formula = NormFormulaMapper.ToDeviationFormula(
            entity.DeviationModelType,
            entity.DeviationCoefficientsJson,
            entity.Performance,
            entity.StandardDeviation);
        return ToDto(formula);
    }
}
