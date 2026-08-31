using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class IicWorkDirectionWorkCostConfiguration : IEntityTypeConfiguration<IicWorkDirectionWorkCost>
{
    public void Configure(EntityTypeBuilder<IicWorkDirectionWorkCost> builder)
    {
        builder.ToTable("IicWorkDirectionWorkCosts", t =>
        {
            t.HasCheckConstraint(
                "CK_IicWorkDirectionWorkCosts_EffectiveRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]");
            t.HasCheckConstraint(
                "CK_IicWorkDirectionWorkCosts_RateNonNegative",
                "[RatePerHour] >= 0");
        });

        builder.HasIndex(c => new { c.WorkDirectionId, c.EffectiveFrom })
            .IsUnique()
            .IsDescending(false, true)
            .HasDatabaseName("UX_IicWorkDirectionWorkCosts_WorkDirection_EffectiveFrom")
            .IncludeProperties(c => new { c.EffectiveTo, c.RatePerHour });

        builder.HasOne(c => c.WorkDirection)
            .WithMany()
            .HasForeignKey(c => c.WorkDirectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
