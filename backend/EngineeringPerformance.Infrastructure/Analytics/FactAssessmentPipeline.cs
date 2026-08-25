using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Единая точка оценки: факт — PLI.PlannedHours; план/прогноз — live N(V)×сценарий.
/// Z всегда через CalculatePerformance / ComputeWeightedZFromPerformanceRows.
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
        var isPlan = bundle.Direction is AnalysisDirection.Plan or AnalysisDirection.Forecast;
        var performance = isPlan
            ? ProjectsPageGridBuilder.BuildPlanPerformance(bundle, _calculationEngine)
            : ProjectsPageGridBuilder.BuildPerformance(bundle, _calculationEngine);

        if (!isPlan)
            _ = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _calculationEngine);

        decimal? totalHours = isPlan
            ? SumRootSubtreeHours(performance)
            : FactLaborHoursTotals.FromLaborIntensities(bundle.LaborIntensities);
        var weightedZ = _calculationEngine.ComputeWeightedZFromPerformanceRows(
            performance,
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces);

        return new AssessmentResultDto(
            request.FormulaId,
            performance,
            new AssessmentSummaryDto(totalHours, weightedZ));
    }

    private static decimal? SumRootSubtreeHours(IReadOnlyList<PerformanceRowDto> performance)
    {
        var total = performance
            .Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId is null)
            .Sum(r => r.SubtreeHours ?? 0m);
        return total > 0 ? Math.Round(total, 2) : null;
    }
}
