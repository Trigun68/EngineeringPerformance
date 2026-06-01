using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectEquipmentConfiguration : IEntityTypeConfiguration<ProjectEquipment>
{
    public void Configure(EntityTypeBuilder<ProjectEquipment> builder)
    {
        builder.ToTable("ProjectEquipments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Quantity)
            .HasPrecision(18, 4);

        builder.Property(e => e.QuantityInWork)
            .HasPrecision(18, 4);

        builder.Property(e => e.PositionNumber).HasMaxLength(50);
        builder.Property(e => e.FacilityCode).HasMaxLength(100);
        builder.Property(e => e.FacilityName).HasMaxLength(500);
        builder.Property(e => e.DocumentSet).HasMaxLength(100);
        builder.Property(e => e.DiagramNumber).HasMaxLength(200);
        builder.Property(e => e.ShortName).HasMaxLength(1000);
        builder.Property(e => e.CatalogName).HasMaxLength(1000);
        builder.Property(e => e.TypeMark).HasMaxLength(200);
        builder.Property(e => e.CatalogId).HasMaxLength(100);
        builder.Property(e => e.Tag).HasMaxLength(100);
        builder.Property(e => e.FullEquipmentId).HasMaxLength(100);
        builder.Property(e => e.SupplyStandard).HasMaxLength(200);
        builder.Property(e => e.OlReference).HasMaxLength(200);
        builder.Property(e => e.EstimationBasis).HasMaxLength(2000);

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
        builder.HasIndex(e => e.FacilityCode);
    }
}
