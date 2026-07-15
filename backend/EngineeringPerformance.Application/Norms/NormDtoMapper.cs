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

    public static NormFormulaDto ToNormFormulaDto(IicWorkDirectionNorm entity)
    {
        var formula = NormFormulaMapper.ToNormFormula(
            entity.FormulaType,
            entity.CoefficientsJson,
            entity.MinVolume,
            entity.MaxVolume,
            entity.ExtrapolationMode,
            entity.Performance);
        return ToDto(formula);
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
