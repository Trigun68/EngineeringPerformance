using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics.NormRegistry;

/// <summary>
/// Чистый движок health-check нормативной базы (без I/O).
/// STALE_DRAFT промта → STALE_FUTURE (будущие периоды).
/// </summary>
public static class NormHealthCheckEngine
{
    public static HealthSummaryDto Run(
        IReadOnlyList<NormHealthNormInput> norms,
        IReadOnlyDictionary<int, NormUsageHint> usageByNormId,
        NormHealthOptions options,
        DateTime? asOf = null)
    {
        if (!options.Enabled)
            return new HealthSummaryDto(0, 0, 0, []);

        var today = (asOf ?? DateTime.UtcNow).Date;
        var detectedAt = DateTime.UtcNow;
        var issues = new List<NormHealthIssueDto>();

        var byGroup = norms
            .GroupBy(n => n.WorkDirectionId)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.EffectiveFrom).ThenBy(n => n.Id).ToList());

        foreach (var (groupId, groupNorms) in byGroup)
        {
            var groupName = groupNorms[0].GroupName;
            var groupCode = groupNorms[0].GroupCode;
            var groupLabel = string.IsNullOrWhiteSpace(groupCode)
                ? groupName
                : $"{groupCode} — {groupName}";

            if (options.NoActiveNorm.Enabled)
                CheckNoActiveNorm(groupId, groupLabel, groupNorms, today, options.NoActiveNorm.Severity, detectedAt, issues);

            if (options.OverlapConflict.Enabled)
                CheckOverlaps(groupId, groupLabel, groupNorms, options.OverlapConflict.Severity, detectedAt, issues);

            if (options.CoverageGap.Enabled)
                CheckCoverageGaps(groupId, groupLabel, groupNorms, options.CoverageGap.Severity, detectedAt, issues);

            if (options.BrokenChain.Enabled)
                CheckBrokenChain(groupId, groupLabel, groupNorms, options.BrokenChain, detectedAt, issues);

            if (options.StaleActive.Enabled)
                CheckStaleActive(groupId, groupLabel, groupNorms, options.StaleActive.Severity, detectedAt, issues);

            if (options.DuplicateNorm.Enabled)
                CheckDuplicates(groupId, groupLabel, groupNorms, options.DuplicateNorm, detectedAt, issues);
        }

        foreach (var norm in norms)
        {
            if (options.ZeroSigma.Enabled)
                CheckZeroSigma(norm, options.ZeroSigma, detectedAt, issues);

            if (options.InvalidParams.Enabled)
                CheckInvalidParams(norm, options.InvalidParams.Severity, detectedAt, issues);

            if (options.UnusedNorm.Enabled)
                CheckUnused(norm, usageByNormId, today, options.UnusedNorm, detectedAt, issues);

            if (options.StaleFuture.Enabled)
                CheckStaleFuture(norm, today, options.StaleFuture, detectedAt, issues);
        }

        var error = issues.Count(i => i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        var warning = issues.Count(i => i.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase));
        var info = issues.Count(i => i.Severity.Equals("Info", StringComparison.OrdinalIgnoreCase));
        return new HealthSummaryDto(error, warning, info, issues);
    }

    private static void CheckNoActiveNorm(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        DateTime today,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        var covers = groupNorms.Any(n => NormDerivedStatus.Covers(n.EffectiveFrom, n.EffectiveTo, today));
        if (covers) return;

        issues.Add(Issue(
            "NO_ACTIVE_NORM",
            severity,
            [],
            groupId,
            $"В группе «{groupLabel}» нет активной нормы на текущую дату.",
            new { date = today },
            detectedAt));
    }

    private static void CheckOverlaps(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        for (var i = 0; i < groupNorms.Count; i++)
        {
            for (var j = i + 1; j < groupNorms.Count; j++)
            {
                var a = groupNorms[i];
                var b = groupNorms[j];
                if (!NormDerivedStatus.PeriodsOverlap(a.EffectiveFrom, a.EffectiveTo, b.EffectiveFrom, b.EffectiveTo))
                    continue;

                // Смежные периоды без пересечения дат: EffectiveTo_A + 1 day == EffectiveFrom_B — не overlap
                // PeriodsOverlap с inclusive dates: if A ends Jan1 and B starts Jan1 → overlap. Adjacent means A.To+1=B.From
                if (IsAdjacent(a, b)) continue;

                var rangeStart = a.EffectiveFrom > b.EffectiveFrom ? a.EffectiveFrom : b.EffectiveFrom;
                var aEnd = a.EffectiveTo ?? DateTime.MaxValue.Date;
                var bEnd = b.EffectiveTo ?? DateTime.MaxValue.Date;
                var rangeEnd = aEnd < bEnd ? aEnd : bEnd;

                issues.Add(Issue(
                    "OVERLAP_CONFLICT",
                    severity,
                    [a.Id, b.Id],
                    groupId,
                    $"Нормы #{a.Id} и #{b.Id} в группе «{groupLabel}» имеют пересекающиеся периоды: {Fmt(rangeStart)} – {FmtEnd(rangeEnd)}.",
                    new { a.Id, BId = b.Id, From = rangeStart, To = rangeEnd == DateTime.MaxValue.Date ? (DateTime?)null : rangeEnd },
                    detectedAt));
            }
        }
    }

    private static bool IsAdjacent(NormHealthNormInput a, NormHealthNormInput b)
    {
        if (a.EffectiveTo is DateTime aTo && aTo.Date.AddDays(1) == b.EffectiveFrom.Date)
            return true;
        if (b.EffectiveTo is DateTime bTo && bTo.Date.AddDays(1) == a.EffectiveFrom.Date)
            return true;
        return false;
    }

    private static void CheckCoverageGaps(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        for (var i = 0; i < groupNorms.Count - 1; i++)
        {
            var prev = groupNorms[i];
            var next = groupNorms[i + 1];
            if (prev.EffectiveTo is null) continue; // открытый период — gap не между ним и следующим по from

            var gapStart = prev.EffectiveTo.Value.Date.AddDays(1);
            var gapEnd = next.EffectiveFrom.Date.AddDays(-1);
            if (gapStart <= gapEnd)
            {
                issues.Add(Issue(
                    "COVERAGE_GAP",
                    severity,
                    [prev.Id, next.Id],
                    groupId,
                    $"В группе «{groupLabel}» разрыв покрытия: {Fmt(gapStart)} – {Fmt(gapEnd)}.",
                    new { From = gapStart, To = gapEnd },
                    detectedAt));
            }
        }
    }

    private static void CheckBrokenChain(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        NormHealthRuleOptions rule,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        var tolerance = rule.ChainToleranceDays;
        for (var i = 0; i < groupNorms.Count - 1; i++)
        {
            var prev = groupNorms[i];
            var next = groupNorms[i + 1];
            if (prev.EffectiveTo is null) continue;

            var expectedNext = prev.EffectiveTo.Value.Date.AddDays(1);
            var deltaDays = Math.Abs((next.EffectiveFrom.Date - expectedNext).TotalDays);
            if (deltaDays <= tolerance) continue;

            // Gap уже покрыт COVERAGE_GAP; broken chain — когда стык не день-в-день (включая overlap-стык)
            if (next.EffectiveFrom.Date > expectedNext.AddDays(tolerance) ||
                next.EffectiveFrom.Date < expectedNext.AddDays(-tolerance))
            {
                // Skip pure gaps already reported if next starts after gap — still report chain break
                issues.Add(Issue(
                    "BROKEN_CHAIN",
                    rule.Severity,
                    [prev.Id, next.Id],
                    groupId,
                    $"Разрыв цепочки версионирования в группе «{groupLabel}» между #{prev.Id} и #{next.Id}.",
                    new { PrevTo = prev.EffectiveTo, NextFrom = next.EffectiveFrom, ToleranceDays = tolerance },
                    detectedAt));
            }
        }
    }

    private static void CheckStaleActive(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        for (var i = 0; i < groupNorms.Count; i++)
        {
            var n = groupNorms[i];
            if (n.EffectiveTo is not null) continue;
            var hasNewer = groupNorms.Any(o => o.Id != n.Id && o.EffectiveFrom > n.EffectiveFrom);
            if (!hasNewer) continue;

            issues.Add(Issue(
                "STALE_ACTIVE",
                severity,
                [n.Id],
                groupId,
                $"Норма #{n.Id} в группе «{groupLabel}» открыта (EffectiveTo пуст), но есть более новый период. Требуется закрытие.",
                new { n.Id, n.EffectiveFrom },
                detectedAt));
        }
    }

    private static void CheckDuplicates(
        int groupId,
        string groupLabel,
        List<NormHealthNormInput> groupNorms,
        NormHealthRuleOptions rule,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        var tol = rule.DupParamTolerance;
        for (var i = 0; i < groupNorms.Count; i++)
        {
            for (var j = i + 1; j < groupNorms.Count; j++)
            {
                var a = groupNorms[i];
                var b = groupNorms[j];
                if (a.FormulaType != b.FormulaType) continue;
                if (!NormDerivedStatus.PeriodsOverlap(a.EffectiveFrom, a.EffectiveTo, b.EffectiveFrom, b.EffectiveTo)
                    && !IsAdjacent(a, b))
                    continue;
                if (!ParamsSimilar(a.Coefficients, b.Coefficients, tol)) continue;

                issues.Add(Issue(
                    "DUPLICATE_NORM",
                    rule.Severity,
                    [a.Id, b.Id],
                    groupId,
                    $"Нормы #{a.Id} и #{b.Id} в группе «{groupLabel}» похожи (возможный дубликат).",
                    new { Tolerance = tol },
                    detectedAt));
            }
        }
    }

    private static bool ParamsSimilar(
        IReadOnlyDictionary<string, decimal> a,
        IReadOnlyDictionary<string, decimal> b,
        double tol)
    {
        var keys = a.Keys.Union(b.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        if (keys.Count == 0) return true;
        foreach (var key in keys)
        {
            a.TryGetValue(key, out var av);
            b.TryGetValue(key, out var bv);
            // case-insensitive get
            if (!a.ContainsKey(key))
                av = a.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!b.ContainsKey(key))
                bv = b.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

            var denom = Math.Abs((double)bv);
            if (denom < 1e-12)
            {
                if (Math.Abs((double)(av - bv)) > tol) return false;
            }
            else if (Math.Abs((double)(av - bv)) / denom >= tol)
            {
                return false;
            }
        }

        return true;
    }

    private static void CheckZeroSigma(
        NormHealthNormInput norm,
        NormHealthRuleOptions rule,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        var sigma = ResolveSigma(norm);
        if (sigma is null) return;
        if ((double)sigma.Value >= rule.MinValidSigma) return;

        issues.Add(Issue(
            "ZERO_SIGMA",
            rule.Severity,
            [norm.Id],
            norm.WorkDirectionId,
            $"Норма #{norm.Id} имеет σ = {sigma}. Z-оценка невозможна.",
            new { Sigma = sigma, rule.MinValidSigma },
            detectedAt));
    }

    private static decimal? ResolveSigma(NormHealthNormInput norm)
    {
        if (norm.StandardDeviation is { } sd)
            return sd;

        if (norm.DeviationCoefficients.TryGetValue("k", out var k))
            return k;
        if (norm.DeviationCoefficients.TryGetValue("sigma", out var s))
            return s;
        foreach (var kv in norm.DeviationCoefficients)
            return kv.Value;

        return null;
    }

    private static void CheckInvalidParams(
        NormHealthNormInput norm,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        foreach (var (_, v) in norm.Coefficients)
        {
            // decimal не бывает NaN; ловим «мусор» через чрезмерную величину после JSON-маппинга.
            if (Math.Abs(v) > 1_000_000_000_000m)
            {
                AddInvalid(norm, severity, detectedAt, issues);
                return;
            }
        }

        if (norm.FormulaType is FormulaType.Power or FormulaType.Exponential or FormulaType.ExponentialDecay)
        {
            if (TryGetCoeff(norm.Coefficients, "a", out var a) && a <= 0)
            {
                AddInvalid(norm, severity, detectedAt, issues);
                return;
            }
        }
    }

    private static void AddInvalid(
        NormHealthNormInput norm,
        string severity,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        issues.Add(Issue(
            "INVALID_PARAMS",
            severity,
            [norm.Id],
            norm.WorkDirectionId,
            $"Норма #{norm.Id}: недопустимые параметры для формулы {norm.FormulaType}.",
            new { norm.FormulaType },
            detectedAt));
    }

    private static bool TryGetCoeff(IReadOnlyDictionary<string, decimal> coeffs, string key, out decimal value)
    {
        if (coeffs.TryGetValue(key, out value)) return true;
        var match = coeffs.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(match.Key))
        {
            value = match.Value;
            return true;
        }

        value = 0;
        return false;
    }

    private static void CheckUnused(
        NormHealthNormInput norm,
        IReadOnlyDictionary<int, NormUsageHint> usageByNormId,
        DateTime today,
        NormHealthRuleOptions rule,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        if (!NormDerivedStatus.Covers(norm.EffectiveFrom, norm.EffectiveTo, today))
            return;

        usageByNormId.TryGetValue(norm.Id, out var usage);
        var last = usage?.LastVersionDate;
        var windowStart = today.AddDays(-rule.UnusedWindowDays);
        if (last is not null && last.Value.Date >= windowStart)
            return;

        var days = last is null
            ? rule.UnusedWindowDays
            : (int)(today - last.Value.Date).TotalDays;

        issues.Add(Issue(
            "UNUSED_NORM",
            rule.Severity,
            [norm.Id],
            norm.WorkDirectionId,
            $"Норма #{norm.Id} не используется {days} дн.",
            new { LastVersionDate = last, rule.UnusedWindowDays },
            detectedAt));
    }

    private static void CheckStaleFuture(
        NormHealthNormInput norm,
        DateTime today,
        NormHealthRuleOptions rule,
        DateTime detectedAt,
        List<NormHealthIssueDto> issues)
    {
        if (norm.EffectiveFrom.Date <= today) return;
        var waitingDays = (norm.EffectiveFrom.Date - today).TotalDays;
        // «возраст ожидания» от CreatedAt
        var ageDays = (today - norm.CreatedAt.Date).TotalDays;
        if (ageDays < rule.DraftMaxAgeDays) return;

        issues.Add(Issue(
            "STALE_FUTURE",
            rule.Severity,
            [norm.Id],
            norm.WorkDirectionId,
            $"Норма #{norm.Id} с будущей датой действия ({Fmt(norm.EffectiveFrom)}) ожидает уже {(int)ageDays} дн.",
            new { norm.EffectiveFrom, WaitingDays = waitingDays, AgeDays = ageDays },
            detectedAt));
    }

    private static NormHealthIssueDto Issue(
        string rule,
        string severity,
        IReadOnlyList<int> normIds,
        int? groupId,
        string message,
        object? details,
        DateTime detectedAt) =>
        new(
            Guid.NewGuid(),
            rule,
            severity,
            normIds,
            groupId,
            message,
            details,
            detectedAt);

    private static string Fmt(DateTime d) => d.ToString("dd.MM.yyyy");

    private static string FmtEnd(DateTime d) =>
        d == DateTime.MaxValue.Date ? "н.в." : Fmt(d);
}
