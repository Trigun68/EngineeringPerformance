using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Application.Norms;

public static class NormSnapshotFactory
{
    public static NormCalculationSnapshot FromEntity(IicWorkDirectionNorm norm)
    {
        var normFormula = NormFormulaMapper.ToNormFormula(
            norm.FormulaType,
            norm.CoefficientsJson,
            norm.MinVolume,
            norm.MaxVolume,
            norm.ExtrapolationMode,
            norm.Performance);

        var deviationFormula = NormFormulaMapper.ToDeviationFormula(
            norm.DeviationModelType,
            norm.DeviationCoefficientsJson,
            norm.Performance,
            norm.StandardDeviation);

        return new NormCalculationSnapshot(
            norm.Performance,
            norm.Estimate,
            norm.StandardDeviation,
            normFormula,
            deviationFormula);
    }
}
