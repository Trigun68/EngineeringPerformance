using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class LaborIntensityIicBindingConfiguration : IEntityTypeConfiguration<LaborIntensityIicBinding>
{
    public void Configure(EntityTypeBuilder<LaborIntensityIicBinding> builder)
    {
        builder.ToTable("LaborIntensityIicBindings", t =>
        {
            t.HasCheckConstraint(
                "CK_LaborIntensityIicBindings_EffectiveRange",
                "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });

        builder.HasIndex(b => new { b.LaborIntensityId, b.EffectiveFrom })
            .IsUnique()
            .IsDescending(false, true)
            .HasDatabaseName("UX_LaborIntensityIicBindings_Labor_EffectiveFrom")
            .IncludeProperties(b => new { b.EffectiveTo, b.IicWorkDirectionId });

        builder.HasOne(b => b.LaborIntensity)
            .WithMany()
            .HasForeignKey(b => b.LaborIntensityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.IicWorkDirection)
            .WithMany()
            .HasForeignKey(b => b.IicWorkDirectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
