using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class LaborIntensityConfiguration : IEntityTypeConfiguration<LaborIntensity>
{
    public void Configure(EntityTypeBuilder<LaborIntensity> builder)
    {
        builder.ToTable("LaborIntensities");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.DocumentTypeName)
            .HasMaxLength(500);

        builder.Property(d => d.Techas)
            .HasMaxLength(20);

        builder.Property(d => d.NormPpr).HasPrecision(18, 4);
        builder.Property(d => d.NormOpr).HasPrecision(18, 4);
        builder.Property(d => d.NormPd).HasPrecision(18, 4);
        builder.Property(d => d.NormRd).HasPrecision(18, 4);

        builder.HasOne(d => d.Parent)
            .WithMany(d => d.Children)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.IicWorkDirection)
            .WithMany()
            .HasForeignKey(d => d.IicWorkDirectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.Code);
        builder.HasIndex(d => d.ParentId);
        builder.HasIndex(d => d.IicWorkDirectionId);
        builder.HasIndex(d => d.IsArchived);
        builder.HasIndex(d => d.DocumentTypeName);
    }
}
