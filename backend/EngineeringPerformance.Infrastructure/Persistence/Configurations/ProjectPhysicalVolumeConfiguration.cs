using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class ProjectPhysicalVolumeConfiguration : IEntityTypeConfiguration<ProjectPhysicalVolume>
{
    public void Configure(EntityTypeBuilder<ProjectPhysicalVolume> builder)
    {
        builder.ToTable("ProjectPhysicalVolumes");
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Quantity).HasPrecision(18, 4);
        builder.Property(pv => pv.FacilityCode).HasMaxLength(100);
        builder.Property(pv => pv.FacilityName).HasMaxLength(500);
        builder.Property(pv => pv.DocumentSet).HasMaxLength(100);
        builder.Property(pv => pv.WorkId).HasMaxLength(200);

        builder.HasOne(pv => pv.Project)
            .WithMany()
            .HasForeignKey(pv => pv.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.ProjectAnalysisVersion)
            .WithMany(v => v.PhysicalVolumes)
            .HasForeignKey(pv => pv.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.PhysicalVolumeKfo)
            .WithMany(pv => pv.ProjectPhysicalVolumes)
            .HasForeignKey(pv => pv.PhysicalVolumeKfoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.PhysicalVolumeBer)
            .WithMany(pv => pv.ProjectPhysicalVolumes)
            .HasForeignKey(pv => pv.PhysicalVolumeBerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.Parent)
            .WithMany(pv => pv.Children)
            .HasForeignKey(pv => pv.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
