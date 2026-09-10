using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Services;

/// <summary>
/// Оценка факта: точный DocSet×Classifier, иначе ближайший заявленный узел
/// или (если claimed пуст) корзина IIC первого уровня.
/// </summary>
public sealed partial class PerformanceCalculationEngine
{
    public FactAssessmentCalculationResult CalculateFactAssessment(FactAssessmentCalculationInput input)
    {
        var parentById = BuildParentLookup(input);
        var orderedPv = input.PhysicalVolumes
            .Select((pv, index) => (pv, index))
            .OrderBy(x => x.pv.FacilityCode)
            .ThenBy(x => x.pv.FacilityName)
            .ThenBy(x => x.pv.PvCode)
            .Select(x => x.pv)
            .ToList();

        var n = orderedPv.Count;
        var hoursAcc = new decimal?[n];
        var fromExact = new bool[n];
        var fromCoarse = new bool[n];

        var hoursByDocSetDir = input.LaborIntensities
            .Where(li => li.WorkDirectionId.HasValue && !string.IsNullOrEmpty(li.DocSet))
            .GroupBy(li => (Set: NormalizeDocSetKey(li.DocSet), Dir: li.WorkDirectionId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Hours));

        var docSetVolumes = orderedPv
            .Where(pv => pv.IsAssessment && pv.ClassifierId.HasValue && !string.IsNullOrEmpty(pv.DocumentSet))
            .GroupBy(pv => (Set: NormalizeDocSetKey(pv.DocumentSet), Dir: pv.ClassifierId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.Quantity * pv.Coefficient));

        var exactKeysAllocated = new HashSet<(string Set, int Dir)>();

        for (var i = 0; i < n; i++)
        {
            var pv = orderedPv[i];
            var vol = pv.Quantity * pv.Coefficient;
            if (!pv.IsAssessment || !pv.ClassifierId.HasValue || vol <= 0 || string.IsNullOrEmpty(pv.DocumentSet))
                continue;

            var key = (Set: NormalizeDocSetKey(pv.DocumentSet), Dir: pv.ClassifierId.Value);
            if (!docSetVolumes.TryGetValue(key, out var setVol)
                || setVol <= 0
                || !hoursByDocSetDir.TryGetValue(key, out var setHours)
                || setHours <= 0)
            {
                continue;
            }

            hoursAcc[i] = Math.Round(setHours * (vol / setVol), 2);
            fromExact[i] = true;
            exactKeysAllocated.Add(key);
        }

        var claimed = IicClaimedBuckets.IsExclusive(input.ClaimedWorkDirectionIds)
            ? input.ClaimedWorkDirectionIds
            : null;
        var coarseBranded = new Dictionary<(string Set, int L1), decimal>();
        var coarseOpen = new Dictionary<int, decimal>();
        foreach (var li in input.LaborIntensities.Where(x => x.WorkDirectionId.HasValue))
        {
            var dir = li.WorkDirectionId!.Value;
            if (!string.IsNullOrEmpty(li.DocSet)
                && exactKeysAllocated.Contains((NormalizeDocSetKey(li.DocSet), dir)))
                continue;

            var bucket = claimed is not null
                ? IicClaimedBuckets.NearestClaimed(dir, claimed, parentById) ?? dir
                : IicHierarchy.ToFirstLevelId(dir, parentById);
            if (!string.IsNullOrEmpty(li.DocSet))
            {
                var key = (li.DocSet, bucket);
                coarseBranded[key] = coarseBranded.GetValueOrDefault(key) + li.Hours;
            }
            else
            {
                coarseOpen[bucket] = coarseOpen.GetValueOrDefault(bucket) + li.Hours;
            }
        }

        AllocateCoarseBuckets(
            orderedPv,
            hoursAcc,
            fromExact,
            fromCoarse,
            coarseBranded,
            parentById,
            matchDocumentSet: true,
            claimed);

        AllocateCoarseOpen(
            orderedPv,
            hoursAcc,
            fromExact,
            fromCoarse,
            coarseOpen,
            parentById,
            claimed);

        var detailRows = new List<FactAssessmentDetailRowDto>();

        for (var i = 0; i < n; i++)
        {
            var pv = orderedPv[i];
            var vol = pv.Quantity * pv.Coefficient;
            var normKey = fromCoarse[i] && pv.ClassifierId is int cid
                ? BucketOf(cid, claimed, parentById)
                : pv.ClassifierId ?? -1;
            input.NormByDirection.TryGetValue(normKey, out var norm);
            if (norm is null && pv.ClassifierId is int rowCid)
                input.NormByDirection.TryGetValue(rowCid, out norm);

            decimal? factHours = hoursAcc[i] is > 0 ? hoursAcc[i] : null;
            decimal? factProductivity = null;
            decimal? zScore = null;
            if (factHours is > 0 && vol > 0)
            {
                factProductivity = Math.Max(0m, Math.Round(factHours.Value / vol, 4));
                if (factProductivity > 0
                    && norm?.NormEstimate is decimal ne
                    && norm.NormStdDev is decimal nsd
                    && nsd > 0)
                {
                    var factEstimate = ProductivityToEstimate(factProductivity.Value);
                    zScore = Math.Round((factEstimate - ne) / nsd, 4);
                }
            }

            detailRows.Add(new FactAssessmentDetailRowDto(
                pv.FacilityCode,
                pv.FacilityName,
                pv.DocumentSet,
                pv.PvCode,
                pv.PvName,
                pv.WorkId,
                pv.WorkName,
                FormatClassifierName(pv.ClassifierId, input.ClassifierMap),
                norm?.NormUnitName ?? string.Empty,
                pv.Quantity,
                factHours,
                factProductivity,
                zScore,
                pv.SourceRowId,
                IsEquipment: false));
        }

        // Оборудование: только объём в гриде. Факт. трудоёмкость — исключительно из LI
        // (доля DocSet×направление на PPV). Не считать vol×NormProductivity — это формула прогноза.
        foreach (var eq in input.Equipment
                     .OrderBy(e => e.FacilityCode)
                     .ThenBy(e => e.FacilityName)
                     .ThenBy(e => e.PvName))
        {
            input.NormByDirection.TryGetValue(eq.ClassifierId ?? -1, out var norm);

            detailRows.Add(new FactAssessmentDetailRowDto(
                eq.FacilityCode,
                eq.FacilityName,
                eq.DocumentSet,
                eq.PvCode,
                eq.PvName,
                string.Empty,
                string.Empty,
                FormatClassifierName(eq.ClassifierId, input.ClassifierMap),
                norm?.NormUnitName ?? string.Empty,
                eq.Quantity,
                FactLaborHours: null,
                FactProductivity: null,
                ZScore: null,
                eq.SourceRowId,
                IsEquipment: true));
        }

        var facilityRows = detailRows
            .Where(r => !string.IsNullOrEmpty(r.FacilityCode))
            .GroupBy(r => r.FacilityCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                decimal? totalHours = g.Any(r => r.FactLaborHours.HasValue)
                    ? Math.Round(g.Sum(r => r.FactLaborHours ?? 0m), 2)
                    : null;

                var facilityZ = ComputeWeightedZ(
                    g.Select(r => (r.ZScore, r.FactLaborHours)),
                    decimalPlaces: 4);

                var interp = PerformanceInterpretationMapper.FromZScore(facilityZ);
                return new FactAssessmentFacilityRowDto(
                    g.Key,
                    first.FacilityName,
                    totalHours,
                    facilityZ,
                    interp?.ToRussian() ?? string.Empty);
            })
            .ToList();

        var totalLiHoursWithoutDirection = input.LaborIntensities
            .Where(li => !li.WorkDirectionId.HasValue)
            .Sum(li => li.Hours);
        var unmatchedHours = Math.Round(totalLiHoursWithoutDirection, 2);
        if (unmatchedHours > 0)
        {
            facilityRows.Add(new FactAssessmentFacilityRowDto(
                string.Empty,
                "(пусто)",
                unmatchedHours,
                null,
                string.Empty));

            detailRows.Add(new FactAssessmentDetailRowDto(
                string.Empty,
                "(пусто)",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0m,
                unmatchedHours,
                null,
                null,
                SourceRowId: null,
                IsEquipment: false));
        }

        return new FactAssessmentCalculationResult(detailRows, facilityRows);
    }

    private static Dictionary<int, int?> BuildParentLookup(FactAssessmentCalculationInput input)
    {
        var parentById = new Dictionary<int, int?>();
        foreach (var kv in input.ClassifierMap)
            parentById[kv.Key] = kv.Value.ParentId;
        foreach (var pv in input.PhysicalVolumes)
        {
            if (pv.ClassifierId is int cid)
                parentById.TryAdd(cid, null);
        }

        foreach (var li in input.LaborIntensities)
        {
            if (li.WorkDirectionId is int dir)
                parentById.TryAdd(dir, null);
        }

        return parentById;
    }

    private static void AllocateCoarseBuckets(
        IReadOnlyList<FactPhysicalVolumeLine> orderedPv,
        decimal?[] hoursAcc,
        bool[] fromExact,
        bool[] fromCoarse,
        Dictionary<(string Set, int L1), decimal> buckets,
        IReadOnlyDictionary<int, int?> parentById,
        bool matchDocumentSet,
        IReadOnlySet<int>? claimed = null)
    {
        foreach (var ((set, l1), setHours) in buckets)
        {
            if (setHours <= 0)
                continue;

            decimal setVol = 0m;
            var indices = new List<int>();
            for (var i = 0; i < orderedPv.Count; i++)
            {
                if (fromExact[i])
                    continue;
                var pv = orderedPv[i];
                if (!pv.IsAssessment || pv.ClassifierId is not int cid)
                    continue;
                if (BucketOf(cid, claimed, parentById) != l1)
                    continue;
                if (matchDocumentSet
                    && !string.Equals(pv.DocumentSet, set, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var vol = pv.Quantity * pv.Coefficient;
                if (vol <= 0)
                    continue;
                setVol += vol;
                indices.Add(i);
            }

            if (setVol <= 0 || indices.Count == 0)
                continue;

            foreach (var i in indices)
            {
                var pv = orderedPv[i];
                var vol = pv.Quantity * pv.Coefficient;
                hoursAcc[i] = (hoursAcc[i] ?? 0m) + Math.Round(setHours * (vol / setVol), 2);
                fromCoarse[i] = true;
            }
        }
    }

    private static void AllocateCoarseOpen(
        IReadOnlyList<FactPhysicalVolumeLine> orderedPv,
        decimal?[] hoursAcc,
        bool[] fromExact,
        bool[] fromCoarse,
        Dictionary<int, decimal> hoursByL1,
        IReadOnlyDictionary<int, int?> parentById,
        IReadOnlySet<int>? claimed = null)
    {
        var buckets = hoursByL1.ToDictionary(kv => (Set: string.Empty, L1: kv.Key), kv => kv.Value);
        AllocateCoarseBuckets(
            orderedPv,
            hoursAcc,
            fromExact,
            fromCoarse,
            buckets,
            parentById,
            matchDocumentSet: false,
            claimed);
    }

    private static int BucketOf(
        int classifierId,
        IReadOnlySet<int>? claimed,
        IReadOnlyDictionary<int, int?> parentById) =>
        claimed is not null
            ? IicClaimedBuckets.NearestClaimed(classifierId, claimed, parentById) ?? classifierId
            : IicHierarchy.ToFirstLevelId(classifierId, parentById);

    private static string NormalizeDocSetKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    private static string FormatClassifierName(
        int? classifierId,
        IReadOnlyDictionary<int, ClassifierDisplayNode> classifierMap)
    {
        if (classifierId is not int cid || !classifierMap.TryGetValue(cid, out var node))
            return string.Empty;
        return IicWorkDirectionDisplayUnit.FormatCodeNameWithUnit(node.Code, node.Name, node.UnitName);
    }
}
