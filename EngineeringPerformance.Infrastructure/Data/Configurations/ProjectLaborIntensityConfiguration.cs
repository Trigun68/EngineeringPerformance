using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectLaborIntensityConfiguration : IEntityTypeConfiguration<ProjectLaborIntensity>
{
    public void Configure(EntityTypeBuilder<ProjectLaborIntensity> builder)
    {
        builder.ToTable("ProjectLaborIntensities");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.DocumentationCode)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(li => li.PlannedHours)
            .HasPrecision(18, 4);

        builder.Property(li => li.ActualHours)
            .HasPrecision(18, 4);

        builder.Property(li => li.Notes)
            .HasMaxLength(2000);

        builder.Property(li => li.Brand)
            .HasMaxLength(100);

        builder.HasOne(li => li.Project)
            .WithMany()
            .HasForeignKey(li => li.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.ProjectAnalysisVersion)
            .WithMany(v => v.LaborIntensities)
            .HasForeignKey(li => li.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(li => li.WorkDirection)
            .WithMany()
            .HasForeignKey(li => li.WorkDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.LaborIntensity)
            .WithMany()
            .HasForeignKey(li => li.LaborIntensityId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(li => li.PhysicalVolumeKfo)
            .WithMany(pv => pv.LaborIntensities)
            .HasForeignKey(li => li.PhysicalVolumeKfoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(li => li.PhysicalVolumeBer)
            .WithMany(pv => pv.LaborIntensities)
            .HasForeignKey(li => li.PhysicalVolumeBerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(li => li.ProjectPhysicalVolume)
            .WithMany()
            .HasForeignKey(li => li.ProjectPhysicalVolumeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(li => li.Brand);
        builder.HasIndex(li => li.LaborIntensityId);
        builder.HasIndex(li => li.PhysicalVolumeKfoId);
        builder.HasIndex(li => li.PhysicalVolumeBerId);
        builder.HasIndex(li => new { li.ProjectAnalysisVersionId, li.ProjectId });
    }
}
