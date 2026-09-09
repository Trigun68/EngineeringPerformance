using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>
/// Регистр сведений: группа ИИС позиции справочника трудоёмкости с даты.
/// </summary>
public sealed class LaborIntensityIicBinding : RegisterEntry
{
    public int LaborIntensityId { get; set; }

    public int? IicWorkDirectionId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public LaborIntensity LaborIntensity { get; set; } = null!;

    public IicWorkDirection? IicWorkDirection { get; set; }
}
