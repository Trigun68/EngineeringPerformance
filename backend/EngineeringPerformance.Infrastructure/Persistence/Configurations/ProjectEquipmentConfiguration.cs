using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class ProjectEquipmentConfiguration : IEntityTypeConfiguration<ProjectEquipment>
{
    public void Configure(EntityTypeBuilder<ProjectEquipment> builder)
    {
        builder.ToTable("ProjectEquipments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity).HasPrecision(18, 4);
        builder.Property(e => e.QuantityInWork).HasPrecision(18, 4);
        builder.Property(e => e.PositionNumber).HasMaxLength(50);
        builder.Property(e => e.FacilityCode).HasMaxLength(100);
        builder.Property(e => e.DocumentSet).HasMaxLength(100);
        builder.Property(e => e.CatalogId).HasMaxLength(100);
        builder.Property(e => e.Tag).HasMaxLength(100);
        builder.Property(e => e.FullEquipmentId).HasMaxLength(100);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProjectAnalysisVersion)
            .WithMany(v => v.Equipments)
            .HasForeignKey(e => e.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Equipment)
            .WithMany(e => e.ProjectEquipments)
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => e.ProjectAnalysisVersionId);
        builder.HasIndex(e => e.EquipmentId);
        // Hot path: /performance, import replace (SqlQueryMetrics → 110_HotPathIndexes.sql)
        builder.HasIndex(e => new { e.ProjectAnalysisVersionId, e.EquipmentId })
            .HasDatabaseName("IX_ProjectEquipments_VersionId_EquipmentId");
    }
}
