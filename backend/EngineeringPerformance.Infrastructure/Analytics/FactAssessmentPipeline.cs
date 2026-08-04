using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Единая точка оценки факта через registry стратегий (UI-агностично к формуле).
/// </summary>
public sealed class FactAssessmentPipeline : IFactAssessmentPipeline
{
    private readonly ProjectsPageRawDataLoader _rawLoader;
    private readonly IPerformanceCalculationEngine _calculationEngine;

    public FactAssessmentPipeline(
        ProjectsPageRawDataLoader rawLoader,
        IPerformanceCalculationEngine calculationEngine)
    {
        _rawLoader = rawLoader;
        _calculationEngine = calculationEngine;
    }

    public IReadOnlyList<AssessmentFormulaDescriptor> ListFormulas() =>
    [
        new(
            DocSetProportionalFormula.Id,
            DocSetProportionalFormula.DisplayName,
            "Распределение трудоёмкости по DocSet и расчёт Z-score через PerformanceCalculationEngine.",
            [])
    ];

    public async Task<AssessmentResultDto> EvaluateAsync(
        AssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FormulaId != DocSetProportionalFormula.Id)
            throw new InvalidOperationException("Неизвестная формула оценки.");

        var bundle = await _rawLoader.LoadAsync(request.ProjectId, request.VersionId, cancellationToken);
        var performance = ProjectsPageGridBuilder.BuildPerformance(bundle, _calculationEngine);
        _ = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _calculationEngine);

        // SoT часов = сумма PlannedHours PLI (как ProjectVersionAnalysisService / PDF анализа).
        var totalHours = FactLaborHoursTotals.FromLaborIntensities(bundle.LaborIntensities);
        var weightedZ = _calculationEngine.ComputeWeightedZFromPerformanceRows(
            performance,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);

        return new AssessmentResultDto(
            request.FormulaId,
            performance,
            new AssessmentSummaryDto(totalHours, weightedZ));
    }
}
