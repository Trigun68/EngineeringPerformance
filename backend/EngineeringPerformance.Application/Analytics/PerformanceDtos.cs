using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Analytics;

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
    PerformanceInterpretation? Interpretation,
    /// <summary>Часы поддерева, участвующие в средневзвешенной FactEstimate (и в весе Z родителя).</summary>
    decimal? SubtreeHours = null,
    /// <summary>Синтетическая строка «Итого» / «ИТОГО» (сумма часов и ФО по ЕИ).</summary>
    bool IsTotal = false,
    /// <summary>Отображение ФО на строках итогов: «число (ЕИ); …».</summary>
    string? FactPhysicalVolumeDisplay = null)
{
    /// <summary>Синтетическая строка: сумма физ. объёмов без связи с группой ИИС (ClassifierId).</summary>
    public const int UnallocatedPhysicalVolumeWorkDirectionId = -1;
}
