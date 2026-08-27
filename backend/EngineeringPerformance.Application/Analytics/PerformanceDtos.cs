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
    string? FactPhysicalVolumeDisplay = null,
    /// <summary>Плановые часы пессимистичного сценария (сумма LaborIntensityPessimistic по ClassifierId).</summary>
    decimal? PessimisticHours = null,
    /// <summary>Плановые часы базового сценария (сумма LaborIntensityBase по ClassifierId).</summary>
    decimal? BaseHours = null,
    /// <summary>Плановые часы оптимистичного сценария (сумма LaborIntensityOptimistic по ClassifierId).</summary>
    decimal? OptimisticHours = null)
{
    /// <summary>Синтетическая строка: сумма физ. объёмов без связи с группой ИИС (ClassifierId).</summary>
    public const int UnallocatedPhysicalVolumeWorkDirectionId = -1;
}
