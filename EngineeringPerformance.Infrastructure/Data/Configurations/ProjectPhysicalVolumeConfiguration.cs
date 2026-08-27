using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectPhysicalVolumeConfiguration : IEntityTypeConfiguration<ProjectPhysicalVolume>
{
    public void Configure(EntityTypeBuilder<ProjectPhysicalVolume> builder)
    {
        builder.ToTable("ProjectPhysicalVolumes");

        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Quantity)
            .HasPrecision(18, 4);

        builder.Property(pv => pv.FacilityCode)
            .HasMaxLength(100);

        builder.Property(pv => pv.FacilityName)
            .HasMaxLength(500);

        builder.Property(pv => pv.BindingType)
            .HasMaxLength(200);

        builder.Property(pv => pv.DocumentSet)
            .HasMaxLength(100);

        builder.Property(pv => pv.CatalogId)
            .HasMaxLength(100);

        builder.Property(pv => pv.Tag)
            .HasMaxLength(100);

        builder.Property(pv => pv.WorkComment)
            .HasMaxLength(500);

        builder.Property(pv => pv.EstimationBasis)
            .HasMaxLength(500);

        builder.Property(pv => pv.WorkId)
            .HasMaxLength(200);

        builder.Property(pv => pv.WorkName)
            .HasMaxLength(1000);

        builder.Property(pv => pv.CatalogCode)
            .HasMaxLength(100);

        builder.Property(pv => pv.CatalogName)
            .HasMaxLength(1000);

        builder.Property(pv => pv.LaborHoursPerUnit)
            .HasPrecision(18, 4);

        builder.Property(pv => pv.MachineHoursPerUnit)
            .HasPrecision(18, 4);

        builder.Property(pv => pv.TotalMachineHours)
            .HasPrecision(18, 4);

        builder.Property(pv => pv.Status)
            .HasMaxLength(50);

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

        builder.HasIndex(pv => pv.CatalogId);
        builder.HasIndex(pv => new { pv.ProjectAnalysisVersionId, pv.ProjectId, pv.PhysicalVolumeKfoId });
        builder.HasIndex(pv => new { pv.ProjectAnalysisVersionId, pv.ProjectId, pv.PhysicalVolumeBerId });
        // Covering index for version grid loads (parity → migrations/schema/110_HotPathIndexes.sql)
        builder.HasIndex(pv => new { pv.ProjectAnalysisVersionId, pv.ProjectId })
            .HasDatabaseName("IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering")
            .IncludeProperties(pv => new
            {
                pv.PhysicalVolumeKfoId,
                pv.PhysicalVolumeBerId,
                pv.Quantity,
                pv.LaborIntensityBase,
                pv.LaborIntensityOptimistic,
                pv.LaborIntensityPessimistic,
                pv.FacilityCode,
                pv.FacilityName,
                pv.DocumentSet,
                pv.CatalogCode,
                pv.CatalogId,
                pv.WorkId,
                pv.ParentId,
                pv.BindingType,
            });
    }
}
