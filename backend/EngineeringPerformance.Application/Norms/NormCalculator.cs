using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public sealed class NormCalculator : INormCalculator
{
    public decimal Calculate(NormFormula formula, decimal volume) =>
        NormFormulaEvaluator.Calculate(formula, volume);

    public IReadOnlyList<(decimal V, decimal N)> GenerateCurve(NormFormula formula, int points = 100)
    {
        points = Math.Max(2, points);
        var vMin = formula.Type == FormulaType.Linear ? 0.1m : (formula.MinVolume > 0 ? formula.MinVolume : 0.1m);
        var vMax = formula.MaxVolume > 0 ? formula.MaxVolume : 1000m;
        if (vMax <= vMin) vMax = vMin + 1m;

        var step = (vMax - vMin) / (points - 1);
        var result = new List<(decimal, decimal)>(points);
        for (var i = 0; i < points; i++)
        {
            var v = vMin + step * i;
            result.Add((v, Calculate(formula, v)));
        }

        return result;
    }
}
