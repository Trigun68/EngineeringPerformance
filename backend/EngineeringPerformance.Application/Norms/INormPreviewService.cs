using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Norms;

public sealed record NormPreviewCurvePoint(decimal Volume, decimal N);

public sealed record NormPreviewBandPoint(decimal Volume, decimal Center, decimal Upper, decimal Lower);

public sealed record NormPreviewResult(
    IReadOnlyList<NormPreviewCurvePoint> Curve,
    IReadOnlyList<NormPreviewBandPoint> Band);

/// <summary>
/// Предпросмотр нормы: кривая N(V) и полоса отклонений по DTO формул.
/// Чистый расчёт без БД — дефолты формул применяются здесь, а не в контроллере.
/// </summary>
public interface INormPreviewService
{
    NormPreviewResult Preview(NormPreviewRequest request);
}

public sealed class NormPreviewService(
    INormCalculator normCalculator,
    IDeviationCalculator deviationCalculator) : INormPreviewService
{
    public NormPreviewResult Preview(NormPreviewRequest request)
    {
        var normFormula = NormDtoMapper.ToEntity(request.NormFormula);
        var deviationFormula = NormDtoMapper.ToEntity(request.DeviationFormula);

        if (normFormula.Type == 0)
            normFormula.Type = FormulaType.Linear;
        if (normFormula.MaxVolume <= 0)
            normFormula.MaxVolume = 999_999m;
        if (deviationFormula.ModelType == 0)
            deviationFormula.ModelType = DeviationModelType.Multiplicative;

        var points = request.Points ?? 100;
        var band = deviationCalculator.GenerateBand(normFormula, deviationFormula, points);
        var curve = normCalculator.GenerateCurve(normFormula, points)
            .Select(p => new NormPreviewCurvePoint(p.V, p.N))
            .ToList();

        return new NormPreviewResult(
            curve,
            band.Select(b => new NormPreviewBandPoint(b.Volume, b.Center, b.Upper, b.Lower)).ToList());
    }
}
