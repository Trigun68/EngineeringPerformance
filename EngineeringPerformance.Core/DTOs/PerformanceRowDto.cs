using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

public sealed record PerformanceRowDto(
    int WorkDirectionId,
    string WorkDirectionName,
    string WorkDirectionCode,
    int? ParentId,
    int Level,
    string? NormUnitName,
    decimal? FactPhysicalVolume,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? FactEstimate,
    decimal? NormProductivity,
    decimal? NormEstimate,
    decimal? NormStdDev,
    decimal? ZScore,
    PerformanceInterpretation? Interpretation)
{
    /// <summary>Синтетическая строка: сумма физ. объёмов без связи с группой ИИС (ClassifierId).</summary>
    public const int UnallocatedPhysicalVolumeWorkDirectionId = -1;
}
