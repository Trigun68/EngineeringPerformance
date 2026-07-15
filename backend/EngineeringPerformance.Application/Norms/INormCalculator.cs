using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public interface INormCalculator
{
    decimal Calculate(NormFormula formula, decimal volume);

    IReadOnlyList<(decimal V, decimal N)> GenerateCurve(NormFormula formula, int points = 100);
}
