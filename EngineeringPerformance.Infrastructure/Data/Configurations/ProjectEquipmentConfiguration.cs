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

        // Короткие идентификаторы — фиксированный лимит достаточен для Excel (≤100 символов).
        builder.Property(e => e.PositionNumber).HasMaxLength(50);
        builder.Property(e => e.FacilityCode).HasMaxLength(100);
        builder.Property(e => e.DocumentSet).HasMaxLength(100);
        builder.Property(e => e.CatalogId).HasMaxLength(100);
        builder.Property(e => e.Tag).HasMaxLength(100);
        builder.Property(e => e.FullEquipmentId).HasMaxLength(100);

        // Описательные поля — без лимита (в файле «Наименование по каталогу» до 2308 символов).
        builder.Property(e => e.FacilityName);
        builder.Property(e => e.DiagramNumber);
        builder.Property(e => e.ShortName);
        builder.Property(e => e.CatalogName);
        builder.Property(e => e.TypeMark);
        builder.Property(e => e.SupplyStandard);
        builder.Property(e => e.OlReference);
        builder.Property(e => e.EstimationBasis);

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
