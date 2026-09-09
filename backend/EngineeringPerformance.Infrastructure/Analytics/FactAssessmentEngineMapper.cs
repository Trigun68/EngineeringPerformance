using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

internal sealed record FactLaborIntensityRow(string DocSet, int? WorkDirectionId, decimal Hours);

internal sealed class FactFacilityRowInternal
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal? FactPhysicalVolume { get; init; }
    public decimal? PlannedLaborHoursBase { get; init; }
    public decimal? PlannedLaborHoursOptimistic { get; init; }
    public decimal? PlannedLaborHoursPessimistic { get; init; }
    public decimal? FactLaborHours { get; init; }
    public decimal? ZScore { get; init; }
    public string Interpretation { get; init; } = string.Empty;
}

internal sealed class FactDetailRowInternal
{
    public int? RowId { get; init; }
    public bool IsEquipment { get; init; }
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
    public decimal? PlannedLaborHoursBase { get; init; }
    public decimal? PlannedLaborHoursOptimistic { get; init; }
    public decimal? PlannedLaborHoursPessimistic { get; init; }
    public decimal? FactLaborHours { get; init; }
    public decimal? FactProductivity { get; init; }
    public decimal? ZScore { get; init; }
    public string BindingType { get; init; } = string.Empty;
    public string CatalogId { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public string WorkComment { get; init; } = string.Empty;
    public string EstimationBasis { get; init; } = string.Empty;
    public string IsAssessment { get; init; } = string.Empty;
    public decimal? Coefficient { get; init; }
    public decimal? NormProductivity { get; init; }
    public decimal? NormStdDev { get; init; }
}

/// <summary>
/// Маппинг EF → DTO движка оценки факта и обратно. Математика только в PerformanceCalculationEngine.
/// </summary>
internal static class FactAssessmentEngineMapper
{
    public static FactAssessmentCalculationInput ToInput(
        IReadOnlyList<ProjectPhysicalVolume> physicalVolumes,
        IReadOnlyList<ProjectEquipment> equipment,
        IReadOnlyList<FactLaborIntensityRow> laborIntensities,
        IReadOnlyDictionary<int, PerformanceRowDto> normByDirection,
        IReadOnlyDictionary<int, IicWorkDirection> classifierMap,
        IReadOnlySet<int>? claimedWorkDirectionIds = null)
    {
        var classifierDtoMap = classifierMap.ToDictionary(
            kv => kv.Key,
            kv => new ClassifierDisplayNode(
                kv.Value.Id,
                kv.Value.Code,
                kv.Value.Name,
                IicWorkDirectionDisplayUnit.ResolveUnitName(kv.Value, classifierMap),
                kv.Value.ParentId));

        return new FactAssessmentCalculationInput(
            physicalVolumes
                .Select(pv => new FactPhysicalVolumeLine(
                    pv.FacilityCode ?? string.Empty,
                    pv.FacilityName ?? string.Empty,
                    pv.DocumentSet ?? string.Empty,
                    pv.WorkId ?? string.Empty,
                    pv.WorkName ?? string.Empty,
                    pv.GetVolumeCode(),
                    pv.GetVolumeName(),
                    pv.GetClassifierId(),
                    pv.GetIsAssessment() == true,
                    pv.Quantity,
                    pv.GetCoefficient(),
                    pv.Id))
                .ToList(),
            equipment
                .Select(eq =>
                {
                    var k = eq.Equipment?.Coefficient ?? 1m;
                    if (k <= 0m)
                        k = 1m;
                    return new FactEquipmentLine(
                        eq.FacilityCode ?? string.Empty,
                        eq.FacilityName ?? string.Empty,
                        eq.DocumentSet ?? string.Empty,
                        eq.CatalogId ?? string.Empty,
                        eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                        eq.Equipment?.ClassifierId,
                        eq.Equipment?.IsAssessment == true,
                        eq.Quantity,
                        k,
                        eq.Id);
                })
                .ToList(),
            laborIntensities
                .Select(li => new FactLaborIntensityLine(li.DocSet, li.WorkDirectionId, li.Hours))
                .ToList(),
            normByDirection,
            classifierDtoMap,
            claimedWorkDirectionIds);
    }

    public static (List<FactDetailRowInternal> Detail, List<FactFacilityRowInternal> Facility) FromResult(
        FactAssessmentCalculationResult result)
    {
        var detail = result.Detail
            .Select(r => new FactDetailRowInternal
            {
                RowId = r.SourceRowId,
                IsEquipment = r.IsEquipment,
                FacilityCode = r.FacilityCode,
                FacilityName = r.FacilityName,
                DocumentSet = r.DocumentSet,
                PvCode = r.PvCode,
                PvName = r.PvName,
                WorkId = r.WorkId,
                WorkName = r.WorkName,
                IicGroupName = r.IicGroupName,
                IicUnitName = r.IicUnitName,
                Quantity = r.Quantity,
                FactLaborHours = r.FactLaborHours,
                FactProductivity = r.FactProductivity,
                ZScore = r.ZScore,
            })
            .ToList();

        var facility = result.Facility
            .Select(r => new FactFacilityRowInternal
            {
                Code = r.Code,
                Name = r.Name,
                FactLaborHours = r.FactLaborHours,
                ZScore = r.ZScore,
                Interpretation = r.Interpretation,
            })
            .ToList();

        return (detail, facility);
    }
}
