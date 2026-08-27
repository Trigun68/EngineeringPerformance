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
        return EvaluateBundle(request, bundle);
    }

    public async Task<BatchAssessmentResult> EvaluateManyAsync(
        IReadOnlyList<AssessmentRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, AssessmentResultDto>();
        var errors = new Dictionary<int, string>();
        if (requests.Count == 0)
            return new BatchAssessmentResult(results, errors);

        foreach (var request in requests)
        {
            if (request.FormulaId != DocSetProportionalFormula.Id)
                errors[request.VersionId] = "Неизвестная формула оценки.";
        }

        var valid = requests
            .Where(r => r.FormulaId == DocSetProportionalFormula.Id)
            .GroupBy(r => r.VersionId)
            .Select(g => g.First())
            .ToList();

        // Чанки ограничивают пик памяти: bundle крупной версии — десятки тысяч строк ФО.
        const int chunkSize = 8;
        foreach (var chunk in valid.Chunk(chunkSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<int, ProjectsPageVersionBundle> bundles;
            try
            {
                bundles = await _rawLoader.LoadManyAsync(
                    chunk.Select(r => (r.ProjectId, r.VersionId)).ToList(),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                foreach (var request in chunk)
                    errors[request.VersionId] = ex.Message;
                continue;
            }

            foreach (var request in chunk)
            {
                if (!bundles.TryGetValue(request.VersionId, out var bundle))
                {
                    errors[request.VersionId] = "Версия недоступна или не найдена.";
                    continue;
                }

                try
                {
                    results[request.VersionId] = EvaluateBundle(request, bundle);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors[request.VersionId] = ex.Message;
                }
            }
        }

        return new BatchAssessmentResult(results, errors);
    }

    private AssessmentResultDto EvaluateBundle(
        AssessmentRequest request,
        ProjectsPageVersionBundle bundle)
    {
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
