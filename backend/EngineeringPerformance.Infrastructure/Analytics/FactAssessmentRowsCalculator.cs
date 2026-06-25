using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Analytics;

internal sealed record FactLaborIntensityRow(string DocSet, int? WorkDirectionId, decimal Hours);

internal sealed class FactFacilityRowInternal
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal? FactLaborHours { get; init; }
    public decimal? ZScore { get; init; }
    public string Interpretation { get; init; } = string.Empty;
}

internal sealed class FactDetailRowInternal
{
    public string FacilityCode { get; init; } = string.Empty;
    public string FacilityName { get; init; } = string.Empty;
    public string DocumentSet { get; init; } = string.Empty;
    public string PvCode { get; init; } = string.Empty;
    public string PvName { get; init; } = string.Empty;
    public string WorkId { get; init; } = string.Empty;
    public string WorkName { get; init; } = string.Empty;
    public string IicGroupName { get; init; } = string.Empty;
    public string IicUnitName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal? FactLaborHours { get; init; }
    public decimal? FactProductivity { get; init; }
    public decimal? ZScore { get; init; }
}

/// <summary>
/// Расчёт строк «По объектам» и «Детально» для оценки факта (порт из WPF ProjectsPageViewModel).
/// </summary>
internal static class FactAssessmentRowsCalculator
{
    public static (List<FactDetailRowInternal> Detail, List<FactFacilityRowInternal> Facility) Compute(
        IReadOnlyList<ProjectPhysicalVolume> physicalVolumesAll,
        IReadOnlyList<ProjectEquipment> equipmentAll,
        IReadOnlyList<FactLaborIntensityRow> laborIntensities,
        IReadOnlyDictionary<int, PerformanceRowDto> normByDirection,
        IReadOnlyDictionary<int, IicWorkDirection> classifierMap)
    {
        var hoursByDocSetDir = laborIntensities
            .Where(li => li.WorkDirectionId.HasValue && !string.IsNullOrEmpty(li.DocSet))
            .GroupBy(li => (Set: li.DocSet, Dir: li.WorkDirectionId!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(li => li.Hours));

        var docSetVolumes = physicalVolumesAll
            .Where(pv => pv.GetIsAssessment() == true
                         && pv.GetClassifierId().HasValue
                         && !string.IsNullOrEmpty(pv.DocumentSet))
            .GroupBy(pv => (
                Set: pv.DocumentSet!,
                Dir: pv.GetClassifierId()!.Value))
            .ToDictionary(g => g.Key, g => g.Sum(pv => pv.Quantity * pv.GetCoefficient()));

        var detailRows = new List<FactDetailRowInternal>();
        decimal totalMatchedHours = 0m;

        foreach (var pv in physicalVolumesAll
                     .OrderBy(pv => pv.FacilityCode ?? string.Empty)
                     .ThenBy(pv => pv.FacilityName ?? string.Empty)
                     .ThenBy(pv => pv.GetVolumeCode()))
        {
            var dirId = pv.GetClassifierId();
            normByDirection.TryGetValue(dirId ?? -1, out var norm);

            var isAssessment = pv.GetIsAssessment() == true;
            var vol = pv.Quantity * (pv.GetCoefficient());

            decimal? factHours = null;
            decimal? factProductivity = null;
            decimal? zScore = null;
            var interpretationText = string.Empty;

            if (isAssessment && dirId.HasValue && vol > 0 && !string.IsNullOrEmpty(pv.DocumentSet))
            {
                var key = (Set: pv.DocumentSet!, Dir: dirId.Value);
                if (docSetVolumes.TryGetValue(key, out var setVol)
                    && setVol > 0
                    && hoursByDocSetDir.TryGetValue(key, out var setHours)
                    && setHours > 0)
                {
                    factHours = Math.Round(setHours * (vol / setVol), 2);
                    totalMatchedHours += factHours.Value;
                    factProductivity = Math.Round(setHours / setVol, 4);
                    if (factProductivity > 0
                        && norm?.NormEstimate is decimal ne
                        && norm.NormStdDev is decimal nsd
                        && nsd > 0)
                    {
                        var factEstimate = (decimal)Math.Log((double)factProductivity.Value);
                        zScore = Math.Round((factEstimate - ne) / nsd, 4);
                        interpretationText = PerformanceInterpretationHelper.GetInterpretation(zScore)?.ToRussian() ?? string.Empty;
                    }
                }
            }

            detailRows.Add(new FactDetailRowInternal
            {
                FacilityCode = pv.FacilityCode ?? string.Empty,
                FacilityName = pv.FacilityName ?? string.Empty,
                DocumentSet = pv.DocumentSet ?? string.Empty,
                PvCode = pv.GetVolumeCode(),
                PvName = pv.GetVolumeName(),
                WorkId = pv.WorkId ?? string.Empty,
                WorkName = pv.WorkName ?? string.Empty,
                IicGroupName = pv.GetClassifierId() is int cid && classifierMap.TryGetValue(cid, out var cl)
                    ? $"{cl.Code} {cl.Name}"
                    : string.Empty,
                IicUnitName = norm?.NormUnitName ?? string.Empty,
                Quantity = pv.Quantity,
                FactLaborHours = factHours,
                FactProductivity = factProductivity,
                ZScore = zScore,
            });
        }

        foreach (var eq in equipmentAll
                     .OrderBy(e => e.FacilityCode ?? string.Empty)
                     .ThenBy(e => e.FacilityName ?? string.Empty)
                     .ThenBy(e => e.Equipment?.Name ?? string.Empty))
        {
            var dirId = eq.Equipment?.ClassifierId;
            normByDirection.TryGetValue(dirId ?? -1, out var norm);
            var isAssessment = eq.Equipment?.IsAssessment == true;
            var vol = eq.QuantityInIicGroupUnits();
            decimal? factHours = null;
            if (isAssessment && norm?.NormProductivity is decimal p && vol != 0)
                factHours = Math.Round(vol * p, 2);

            detailRows.Add(new FactDetailRowInternal
            {
                FacilityCode = eq.FacilityCode ?? string.Empty,
                FacilityName = eq.FacilityName ?? string.Empty,
                DocumentSet = eq.DocumentSet ?? string.Empty,
                PvCode = eq.CatalogId ?? string.Empty,
                PvName = eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                WorkId = string.Empty,
                WorkName = string.Empty,
                IicGroupName = dirId is int cidEq && classifierMap.TryGetValue(cidEq, out var clEq)
                    ? IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit(clEq, classifierMap)
                    : string.Empty,
                IicUnitName = norm?.NormUnitName ?? string.Empty,
                Quantity = eq.Quantity,
                FactLaborHours = factHours,
            });
        }

        var facilityResult = detailRows
            .Where(r => !string.IsNullOrEmpty(r.FacilityCode))
            .GroupBy(r => r.FacilityCode)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                var hoursRows = g.Where(r => r.FactLaborHours.HasValue && r.ZScore.HasValue).ToList();
                decimal? totalHours = g.Any(r => r.FactLaborHours.HasValue)
                    ? Math.Round(g.Sum(r => r.FactLaborHours ?? 0m), 2)
                    : null;

                decimal? facilityZ = null;
                if (hoursRows.Count > 0)
                {
                    var sumHours = hoursRows.Sum(r => r.FactLaborHours!.Value);
                    if (sumHours > 0)
                    {
                        var weighted = hoursRows.Sum(r => r.ZScore!.Value * r.FactLaborHours!.Value);
                        facilityZ = Math.Round(weighted / sumHours, 4);
                    }
                }

                var interp = PerformanceInterpretationHelper.GetInterpretation(facilityZ);
                return new FactFacilityRowInternal
                {
                    Code = g.Key,
                    Name = first.FacilityName,
                    FactLaborHours = totalHours,
                    ZScore = facilityZ,
                    Interpretation = interp?.ToRussian() ?? string.Empty,
                };
            })
            .ToList();

        var totalLiHours = laborIntensities.Sum(li => li.Hours);
        var unmatchedHours = Math.Round(totalLiHours - totalMatchedHours, 2);
        if (unmatchedHours > 0)
        {
            facilityResult.Add(new FactFacilityRowInternal
            {
                Code = string.Empty,
                Name = "(пусто)",
                FactLaborHours = unmatchedHours,
            });
            detailRows.Add(new FactDetailRowInternal
            {
                FacilityName = "(пусто)",
                FactLaborHours = unmatchedHours,
            });
        }

        return (detailRows, facilityResult);
    }
}
