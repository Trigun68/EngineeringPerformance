namespace EngineeringPerformance.Application.Analytics.FactNorm;

/// <summary>Выбор лучшей модели и вердикт относительно текущей нормы.</summary>
public static class ModelSelector
{
    public static FormulaFitResult? SelectBest(
        IReadOnlyDictionary<string, FormulaFitResult> fits,
        FactNormAnalysisOptions options)
    {
        var candidates = fits.Values
            .Where(f => f.Success && f.Metrics.AdjustedR2 >= options.MinAcceptableR2)
            .OrderBy(f => f.Metrics.Bic)
            .ToList();
        return candidates.FirstOrDefault();
    }

    public static (NormVerdict Verdict, bool Significant) Decide(
        CurrentNormFitContext current,
        FormulaFitResult? best,
        FactNormAnalysisOptions options)
    {
        if (best is null)
            return (NormVerdict.NormIsActual, false);

        var currentFit = TryEvaluateCurrentAsFit(current, best);
        var deltaR2 = best.Metrics.R2 - (currentFit?.Metrics.R2 ?? current.CurrentR2 ?? 0);
        var deltaAic = (currentFit?.Metrics.Aic ?? best.Metrics.Aic + options.DeltaAicSignificant + 1) - best.Metrics.Aic;
        var deltaBic = (currentFit?.Metrics.Bic ?? best.Metrics.Bic + options.DeltaBicStrong + 1) - best.Metrics.Bic;

        var sameType = string.Equals(best.Code, current.FormulaCode, StringComparison.OrdinalIgnoreCase);

        if (!sameType)
        {
            var significant = deltaBic > options.DeltaBicStrong
                || (deltaAic > options.DeltaAicSignificant && deltaR2 > options.DeltaR2Threshold);
            return significant
                ? (NormVerdict.ChangeFormulaType, true)
                : (NormVerdict.NormIsActual, false);
        }

        // Одинаковый тип: F-тест для Linear ⊂ Polynomial2, иначе drift параметров.
        if (IsNestedPair(current.FormulaCode, best.Code)
            && currentFit is not null
            && best.Metrics.PointCount > 0)
        {
            var pValue = FitStatistics.NestedFTestPValue(
                currentFit.Metrics.Rss,
                best.Metrics.Rss,
                currentFit.Metrics.ParameterCount,
                best.Metrics.ParameterCount,
                best.Metrics.PointCount);
            if (pValue < options.PValueThreshold && HasParamDrift(current.Parameters, best.Parameters, options.ParamDriftThreshold))
                return (NormVerdict.AdjustParameters, true);
        }

        if (HasParamDrift(current.Parameters, best.Parameters, options.ParamDriftThreshold))
            return (NormVerdict.AdjustParameters, true);

        return (NormVerdict.NormIsActual, false);
    }

    private static bool IsNestedPair(string current, string best) =>
        string.Equals(current, ReferenceFormulaCodes.Linear, StringComparison.OrdinalIgnoreCase)
        && string.Equals(best, ReferenceFormulaCodes.Polynomial2, StringComparison.OrdinalIgnoreCase);

    public static bool HasParamDrift(
        IReadOnlyDictionary<string, double> current,
        IReadOnlyDictionary<string, double> proposed,
        double threshold)
    {
        foreach (var (key, proposedValue) in proposed)
        {
            if (!current.TryGetValue(key, out var cur))
                return true;
            var denom = Math.Abs(cur) < 1e-12 ? 1.0 : Math.Abs(cur);
            if (Math.Abs(proposedValue - cur) / denom > threshold)
                return true;
        }

        return false;
    }

    private static FormulaFitResult? TryEvaluateCurrentAsFit(CurrentNormFitContext current, FormulaFitResult best)
    {
        // Без точек здесь — только метаданные; RSS текущей модели приходит извне через CurrentR2.
        // Для ΔAIC/BIC при отсутствии полного fit используем эвристику на R².
        if (current.CurrentR2 is not { } r2)
            return null;

        var n = best.Metrics.PointCount;
        var p = Math.Max(1, current.Parameters.Count);
        // Эвристика: RSS ≈ (1-R²)·TSS; без TSS берём относительный AIC через R².
        var rssProxy = Math.Max(1e-12, (1 - Math.Clamp(r2, 0, 1)) * n);
        var mse = rssProxy / Math.Max(n, 1);
        var aic = n * Math.Log(Math.Max(mse, 1e-300)) + 2 * p;
        var bic = n * Math.Log(Math.Max(mse, 1e-300)) + p * Math.Log(Math.Max(n, 1));
        var metrics = new FitMetrics(r2, r2, aic, bic, 0, Math.Sqrt(mse), rssProxy, p, n);
        return new FormulaFitResult(current.FormulaCode, current.Parameters, metrics, true);
    }
}
