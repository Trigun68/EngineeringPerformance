using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public class IicWorkDirectionNormConfiguration : IEntityTypeConfiguration<IicWorkDirectionNorm>
{
    public void Configure(EntityTypeBuilder<IicWorkDirectionNorm> builder)
    {
        builder.ToTable("IicWorkDirectionNorms");

        // Covering unique: as-of lookup by WorkDirection + EffectiveFrom (111_SqlMetricsWave2Indexes.sql)
        builder.HasIndex(n => new { n.WorkDirectionId, n.EffectiveFrom })
            .IsUnique()
            .IsDescending(false, true)
            .HasDatabaseName("UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom")
            .IncludeProperties(n => new
            {
                n.EffectiveTo,
                n.UnitId,
                n.Performance,
                n.Estimate,
                n.StandardDeviation,
                n.FormulaType,
                n.CoefficientsJson,
                n.MinVolume,
                n.MaxVolume,
                n.ExtrapolationMode,
                n.DeviationModelType,
                n.DeviationCoefficientsJson,
            });

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_IicWorkDirectionNorms_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
    }
}
