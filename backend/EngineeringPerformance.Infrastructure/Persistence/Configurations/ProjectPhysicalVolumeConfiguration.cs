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

        // Снимок каталога на момент привязки (116_PpvCatalogSnapshot.sql): коэффициент,
        // признак оценки и классификатор IIC фиксируются в строке, чтобы правка каталога
        // не меняла уже посчитанную версию.
        builder.Property(pv => pv.CatalogCoefficient).HasPrecision(18, 4);
        builder.Property(pv => pv.CatalogIsAssessment).HasColumnName("CatalogIsAssessment");
        builder.Property(pv => pv.CatalogClassifierId).HasColumnName("CatalogClassifierId");
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

        builder.HasIndex(pv => pv.ProjectId);
        builder.HasIndex(pv => pv.ParentId);
        builder.HasIndex(pv => new { pv.ProjectAnalysisVersionId, pv.ProjectId, pv.PhysicalVolumeKfoId });
        builder.HasIndex(pv => new { pv.ProjectAnalysisVersionId, pv.ProjectId, pv.PhysicalVolumeBerId });
        // Covering index for version grid loads (SqlQueryMetrics → 110_HotPathIndexes.sql)
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
