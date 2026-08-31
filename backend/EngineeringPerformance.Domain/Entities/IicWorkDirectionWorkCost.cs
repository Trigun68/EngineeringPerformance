using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

/// <summary>Период ставки стоимости работ (₽/ч) по группе ИИС.</summary>
public sealed class IicWorkDirectionWorkCost : RegisterEntry
{
    public int WorkDirectionId { get; set; }

    public decimal RatePerHour { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    public IicWorkDirection WorkDirection { get; set; } = null!;
}
