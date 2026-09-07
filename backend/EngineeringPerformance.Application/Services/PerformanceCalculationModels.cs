using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Services;

/// <summary>Входные данные для расчёта иерархии производительности (без EF).</summary>
public sealed record PerformanceCalculationInput(
    bool VersionExists,
    IReadOnlyList<WorkDirectionCalculationNode> WorkDirections,
    IReadOnlyList<PhysicalVolumeCalculationLine> PhysicalVolumes,
    IReadOnlyList<EquipmentCalculationLine> Equipment,
    IReadOnlyList<LaborIntensityCalculationLine> LaborIntensities,
    IReadOnlyDictionary<int, NormCalculationSnapshot> NormsByDirection,
    /// <summary>
    /// Факт PLI без поэлементного ключа: часы поднимаются на IIC первого уровня.
    /// План (сохранённые часы по classifier) — false, иначе сценарии смешаются с L1.
    /// </summary>
    bool RollHoursToFirstLevel = false);

public sealed record WorkDirectionCalculationNode(
    int Id,
    int? ParentId,
    string Name,
    string Code,
    string? NormUnitName);

public sealed record PhysicalVolumeCalculationLine(
    int? ClassifierId,
    bool IsAssessment,
    decimal Quantity,
    decimal Coefficient);

public sealed record EquipmentCalculationLine(
    int? ClassifierId,
    bool IsAssessment,
    decimal Quantity,
    decimal Coefficient);

public sealed record LaborIntensityCalculationLine(
    int? WorkDirectionId,
    decimal PlannedHours);

public sealed record NormCalculationSnapshot(
    decimal? Performance,
    decimal? Estimate,
    decimal? StandardDeviation,
    NormFormula NormFormula,
    DeviationFormula DeviationFormula);

/// <summary>Вход для распределения LI по строкам факта (порт ComputeFactRows).</summary>
public sealed record FactAssessmentCalculationInput(
    IReadOnlyList<FactPhysicalVolumeLine> PhysicalVolumes,
    IReadOnlyList<FactEquipmentLine> Equipment,
    IReadOnlyList<FactLaborIntensityLine> LaborIntensities,
    IReadOnlyDictionary<int, PerformanceRowDto> NormByDirection,
    IReadOnlyDictionary<int, ClassifierDisplayNode> ClassifierMap);

public sealed record FactPhysicalVolumeLine(
    string FacilityCode,
    string FacilityName,
    string DocumentSet,
    string WorkId,
    string WorkName,
    string PvCode,
    string PvName,
    int? ClassifierId,
    bool IsAssessment,
    decimal Quantity,
    decimal Coefficient,
    /// <summary>Id строки PPV для обогащения UI (не участвует в математике).</summary>
    int? SourceRowId = null);

public sealed record FactEquipmentLine(
    string FacilityCode,
    string FacilityName,
    string DocumentSet,
    string PvCode,
    string PvName,
    int? ClassifierId,
    bool IsAssessment,
    decimal Quantity,
    decimal Coefficient,
    /// <summary>Id строки ProjectEquipment для обогащения UI (не участвует в математике).</summary>
    int? SourceRowId = null);

public sealed record FactLaborIntensityLine(
    string DocSet,
    int? WorkDirectionId,
    decimal Hours);

public sealed record ClassifierDisplayNode(
    int Id,
    string Code,
    string Name,
    string? UnitName,
    int? ParentId = null);

public sealed record FactAssessmentDetailRowDto(
    string FacilityCode,
    string FacilityName,
    string DocumentSet,
    string PvCode,
    string PvName,
    string WorkId,
    string WorkName,
    string IicGroupName,
    string IicUnitName,
    decimal Quantity,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? ZScore,
    int? SourceRowId = null,
    bool IsEquipment = false);

public sealed record FactAssessmentFacilityRowDto(
    string Code,
    string Name,
    decimal? FactLaborHours,
    decimal? ZScore,
    string Interpretation);

public sealed record FactAssessmentCalculationResult(
    IReadOnlyList<FactAssessmentDetailRowDto> Detail,
    IReadOnlyList<FactAssessmentFacilityRowDto> Facility);

/// <summary>Часы сценариев по направлению ИИС для Σ(часы × ставка).</summary>
public sealed record WorkCostHourSample(
    int WorkDirectionId,
    decimal? OptimisticHours,
    decimal? BaseHours,
    decimal? PessimisticHours);

/// <summary>Стоимость сценариев, ₽. null метода — ни одной пары часы+ставка.</summary>
public sealed record ScenarioWorkCostResult(
    decimal Optimistic,
    decimal Base,
    decimal Pessimistic);
