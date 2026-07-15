using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public interface IDeviationCalculator
{
    decimal CalculateLaborIntensity(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        decimal volume,
        decimal effectiveVolume,
        decimal zScore);

    IReadOnlyList<NormBandPoint> GenerateBand(
        NormFormula normFormula,
        DeviationFormula deviationFormula,
        int points = 100);
}
