using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class IicWorkDirectionNorm : RegisterEntry
{
    public int WorkDirectionId { get; set; }

    public decimal? Performance { get; set; }

    public decimal? Estimate { get; set; }

    public decimal? StandardDeviation { get; set; }

    public int? UnitId { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    public IicWorkDirection WorkDirection { get; set; } = null!;

    public UnitOfMeasurement? Unit { get; set; }
}
