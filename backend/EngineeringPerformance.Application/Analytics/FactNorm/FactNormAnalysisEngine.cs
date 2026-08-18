namespace EngineeringPerformance.Application.Analytics.FactNorm;

public interface IFactNormAnalysisEngine
{
    FactNormEngineResult Run(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<NormGroupComparisonInput> groups,
        AnalyticsOptions options);
}

/// <summary>Чистый движок сверки факта с нормами и model selection (без HTTP/EF).</summary>
public sealed class FactNormAnalysisEngine : IFactNormAnalysisEngine
{
    private readonly IReadOnlyList<IReferenceFormula> _formulas;

    public FactNormAnalysisEngine(IReadOnlyList<IReferenceFormula>? formulas = null)
    {
        _formulas = formulas ?? ReferenceFormulaCatalog.All;
    }

    public FactNormEngineResult Run(
        DateTime periodStart,
        DateTime periodEnd,
        IReadOnlyList<NormGroupComparisonInput> groups,
        AnalyticsOptions options)
    {
        var warnings = new List<string>();
        var comparisons = new List<NormGroupComparisonResult>();
        var totalPoints = 0;

        foreach (var group in groups)
        {
            var sanitized = PointSanitizer.Sanitize(
                group.Points,
                options.FactNormAnalysis.RemoveOutliers3Sigma,
                out var removed);
            if (removed > 0)
                warnings.Add($"{group.GroupName}: исключено точек при санитизации: {removed}.");

            var points = PointSanitizer.SampleIfNeeded(
                sanitized,
                options.FactNormAnalysis.MaxPointsPerRequest,
                out var sampled);
            if (sampled)
                warnings.Add($"{group.GroupName}: применено сэмплирование до {options.FactNormAnalysis.MaxPointsPerRequest} точек.");

            totalPoints += points.Count;

            var meanFact = points.Count == 0 ? 0 : points.Average(p => p.Y);
            var sigmaFact = ComputeStdDev(points.Select(p => p.Y).ToList());
            var sigmaZero = group.CurrentNorm.StdDeviation <= 0 || double.IsNaN(group.CurrentNorm.StdDeviation);
            var deltaMean = meanFact - group.CurrentNorm.MeanPerformance;
            var deltaSigma = sigmaZero ? 0 : sigmaFact - group.CurrentNorm.StdDeviation;

            var requiresAttention =
                Math.Abs(deltaMean) > options.FactNormAnalysis.ThresholdMeanPct * Math.Max(Math.Abs(group.CurrentNorm.MeanPerformance), 1e-9)
                || (!sigmaZero && Math.Abs(deltaSigma) > options.FactNormAnalysis.ThresholdSigmaPct * Math.Max(Math.Abs(group.CurrentNorm.StdDeviation), 1e-9));

            if (points.Count < options.FactNormAnalysis.MinPointsForFit)
            {
                comparisons.Add(new NormGroupComparisonResult(
                    group.NormId,
                    group.WorkDirectionId,
                    group.GroupName,
                    group.CurrentNorm.FormulaCode,
                    group.CurrentNorm.FormulaCode,
                    deltaMean,
                    deltaSigma,
                    group.CurrentNorm.CurrentR2 ?? 0,
                    0,
                    0,
                    NormVerdict.InsufficientData,
                    requiresAttention,
                    sigmaZero,
                    true,
                    points.Count,
                    points,
                    null,
                    null,
                    "недостаточно данных",
                    group.CurrentNorm.MeanPerformance,
                    meanFact,
                    sigmaFact,
                    group.CurrentNorm.StdDeviation,
                    group.CurrentNorm.Parameters));
                continue;
            }

            var fits = new Dictionary<string, FormulaFitResult>(StringComparer.OrdinalIgnoreCase);
            Parallel.ForEach(_formulas, formula =>
            {
                if (!formula.CanFit(points, options.FactNormAnalysis.MinPointsForFit))
                    return;
                var fit = formula.Fit(points);
                lock (fits)
                    fits[formula.Code] = fit;
            });

            // R² текущей нормы по точкам (если возможно предсказать).
            var currentR2 = ComputeCurrentR2(group.CurrentNorm, points);
            var currentCtx = group.CurrentNorm with { CurrentR2 = currentR2 };

            var best = ModelSelector.SelectBest(fits, options.FactNormAnalysis);
            var (verdict, _) = ModelSelector.Decide(currentCtx, best, options.FactNormAnalysis);

            NormSuggestionDraft? suggestion = null;
            if (verdict is NormVerdict.ChangeFormulaType or NormVerdict.AdjustParameters && best is not null)
            {
                var accuracy = AccuracyLevelCalculator.Compute(periodStart, periodEnd, points.Count, options.Accuracy);
                var confidence = AccuracyLevelCalculator.ToConfidence(
                    accuracy.Level, points.Count, best.Metrics.AdjustedR2, options.Accuracy);
                suggestion = new NormSuggestionDraft(
                    verdict,
                    best.Code,
                    best.Parameters,
                    sigmaFact,
                    meanFact,
                    currentR2,
                    best.Metrics.R2,
                    best.Metrics.R2 - currentR2,
                    confidence,
                    points.Count);
            }

            comparisons.Add(new NormGroupComparisonResult(
                group.NormId,
                group.WorkDirectionId,
                group.GroupName,
                group.CurrentNorm.FormulaCode,
                best?.Code ?? group.CurrentNorm.FormulaCode,
                deltaMean,
                deltaSigma,
                currentR2,
                best?.Metrics.R2 ?? 0,
                (best?.Metrics.R2 ?? 0) - currentR2,
                verdict,
                requiresAttention,
                sigmaZero,
                false,
                points.Count,
                points,
                best,
                suggestion,
                sigmaZero ? "σ=0" : null,
                group.CurrentNorm.MeanPerformance,
                meanFact,
                sigmaFact,
                group.CurrentNorm.StdDeviation,
                group.CurrentNorm.Parameters));
        }

        var accuracyResult = AccuracyLevelCalculator.Compute(periodStart, periodEnd, totalPoints, options.Accuracy);
        return new FactNormEngineResult(accuracyResult, comparisons, warnings);
    }

    private static double ComputeStdDev(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return Math.Sqrt(variance);
    }

    private double ComputeCurrentR2(CurrentNormFitContext current, IReadOnlyList<FactPoint> points)
    {
        try
        {
            var formula = _formulas.FirstOrDefault(f =>
                string.Equals(f.Code, current.FormulaCode, StringComparison.OrdinalIgnoreCase));
            if (formula is null)
                return current.CurrentR2 ?? 0;

            var y = points.Select(p => p.Y).ToList();
            var yHat = points.Select(p => formula.Predict(p.X, current.Parameters)).ToList();
            if (yHat.Any(v => double.IsNaN(v) || double.IsInfinity(v)))
                return current.CurrentR2 ?? 0;
            return FitStatistics.Compute(y, yHat, Math.Max(1, current.Parameters.Count)).R2;
        }
        catch
        {
            return current.CurrentR2 ?? 0;
        }
    }
}
