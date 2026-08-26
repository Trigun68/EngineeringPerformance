using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class IicWorkDirectionNormConfiguration : IEntityTypeConfiguration<IicWorkDirectionNorm>
{
    public void Configure(EntityTypeBuilder<IicWorkDirectionNorm> builder)
    {
        builder.ToTable("IicWorkDirectionNorms");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Performance)
            .HasPrecision(18, 4);

        builder.Property(n => n.Estimate)
            .HasPrecision(18, 4);

        builder.Property(n => n.StandardDeviation)
            .HasPrecision(18, 4);

        builder.HasOne(n => n.WorkDirection)
            .WithMany()
            .HasForeignKey(n => n.WorkDirectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Unit)
            .WithMany(u => u.Norms)
            .HasForeignKey(n => n.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // Covering unique: as-of lookup (parity → 111_SqlMetricsWave2Indexes.sql).
        // Legacy entity has no parametric columns — INCLUDE only scalar fields present here.
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
            });

        // V2-A: «закрытие» окна нормы не может быть раньше её открытия.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_IicWorkDirectionNorms_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
    }
}
