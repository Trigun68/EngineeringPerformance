using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Domain.Entities;

public sealed class IicWorkDirectionNorm : RegisterEntry
{
    public int WorkDirectionId { get; set; }

    [Obsolete("Use parametric NormFormula (CoefficientsJson). Kept for migration compatibility.")]
    public decimal? Performance { get; set; }

    public decimal? Estimate { get; set; }

    [Obsolete("Use DeviationCoefficientsJson (sigma ratio). Kept for migration compatibility.")]
    public decimal? StandardDeviation { get; set; }

    public int? UnitId { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    public FormulaType FormulaType { get; set; } = FormulaType.Linear;

    public string? CoefficientsJson { get; set; }

    public decimal MinVolume { get; set; }

    public decimal MaxVolume { get; set; } = 999_999m;

    public ExtrapolationMode ExtrapolationMode { get; set; } = ExtrapolationMode.LinearFromEdge;

    public DeviationModelType DeviationModelType { get; set; } = DeviationModelType.Multiplicative;

    public string? DeviationCoefficientsJson { get; set; }

    public DateTime? MigratedAt { get; set; }

    public IicWorkDirection WorkDirection { get; set; } = null!;

    public UnitOfMeasurement? Unit { get; set; }
}
