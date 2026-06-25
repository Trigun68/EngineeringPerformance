using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;

namespace EngineeringPerformance.Infrastructure.Analytics;

/// <summary>
/// Маппинг EF-сущностей в DTO движка расчёта факта.
/// </summary>
internal static class FactAssessmentEngineMapper
{
    public static FactAssessmentCalculationInput ToInput(
        IReadOnlyList<ProjectPhysicalVolume> physicalVolumes,
        IReadOnlyList<ProjectEquipment> equipment,
        IReadOnlyList<FactLaborIntensityRow> laborIntensities,
        IReadOnlyDictionary<int, PerformanceRowDto> normByDirection,
        IReadOnlyDictionary<int, IicWorkDirection> classifierMap)
    {
        var classifierDtoMap = classifierMap.ToDictionary(
            kv => kv.Key,
            kv => new ClassifierDisplayNode(
                kv.Value.Id,
                kv.Value.Code,
                kv.Value.Name,
                IicWorkDirectionDisplayUnit.ResolveUnitName(kv.Value, classifierMap)));

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
                    pv.GetCoefficient()))
                .ToList(),
            equipment
                .Select(eq => new FactEquipmentLine(
                    eq.FacilityCode ?? string.Empty,
                    eq.FacilityName ?? string.Empty,
                    eq.DocumentSet ?? string.Empty,
                    eq.CatalogId ?? string.Empty,
                    eq.Equipment?.Name ?? eq.CatalogName ?? string.Empty,
                    eq.Equipment?.ClassifierId,
                    eq.Equipment?.IsAssessment == true,
                    eq.Quantity,
                    eq.Equipment?.Coefficient ?? 1m))
                .ToList(),
            laborIntensities
                .Select(li => new FactLaborIntensityLine(li.DocSet, li.WorkDirectionId, li.Hours))
                .ToList(),
            normByDirection,
            classifierDtoMap);
    }

    public static (List<FactDetailRowInternal> Detail, List<FactFacilityRowInternal> Facility) FromResult(
        FactAssessmentCalculationResult result)
    {
        var detail = result.Detail
            .Select(r => new FactDetailRowInternal
            {
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
