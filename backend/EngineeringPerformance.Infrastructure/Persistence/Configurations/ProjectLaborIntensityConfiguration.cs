using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class ProjectLaborIntensityConfiguration : IEntityTypeConfiguration<ProjectLaborIntensity>
{
    public void Configure(EntityTypeBuilder<ProjectLaborIntensity> builder)
    {
        builder.ToTable("ProjectLaborIntensities");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.DocumentationCode).HasMaxLength(450).IsRequired();
        builder.Property(li => li.PlannedHours).HasPrecision(18, 4);
        builder.Property(li => li.ActualHours).HasPrecision(18, 4);

        builder.HasOne(li => li.Project)
            .WithMany()
            .HasForeignKey(li => li.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.ProjectAnalysisVersion)
            .WithMany(v => v.LaborIntensities)
            .HasForeignKey(li => li.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(li => li.ProjectPhysicalVolume)
            .WithMany()
            .HasForeignKey(li => li.ProjectPhysicalVolumeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.PhysicalVolumeKfo)
            .WithMany(pv => pv.LaborIntensities)
            .HasForeignKey(li => li.PhysicalVolumeKfoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(li => li.PhysicalVolumeBer)
            .WithMany(pv => pv.LaborIntensities)
            .HasForeignKey(li => li.PhysicalVolumeBerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Covering index for version-scoped LI load (SqlQueryMetrics → 111_SqlMetricsWave2Indexes.sql)
        builder.HasIndex(li => li.ProjectAnalysisVersionId)
            .HasDatabaseName("IX_ProjectLaborIntensities_VersionId_Covering")
            .IncludeProperties(li => new
            {
                li.ProjectId,
                li.DocumentationCode,
                li.WorkDirectionId,
                li.PlannedHours,
                li.ActualHours,
                li.Notes,
                li.Brand,
                li.CreatedAt,
                li.ModifiedAt,
                li.PhysicalVolumeKfoId,
                li.PhysicalVolumeBerId,
                li.LaborIntensityId,
                li.ProjectPhysicalVolumeId,
            });
    }
}
