using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.CatalogKeyHash).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Coefficient).HasPrecision(18, 4).HasDefaultValue(1m);

        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.IsArchived);
        builder.HasIndex(e => e.ClassifierId);
        builder.HasIndex(e => e.IsAssessment);
        builder.HasIndex(e => e.UnitId);
        builder.HasIndex(e => e.ProjectId);
        builder.HasIndex(e => new { e.ProjectId, e.IsArchived, e.ParentId, e.SortOrder });

        builder.HasIndex(e => new { e.ProjectId, e.CatalogKeyHash })
            .IsUnique()
            .HasFilter("[ProjectId] IS NOT NULL");

        builder.HasIndex(e => e.CatalogKeyHash)
            .IsUnique()
            .HasFilter("[ProjectId] IS NULL");

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Classifier)
            .WithMany()
            .HasForeignKey(e => e.ClassifierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Unit)
            .WithMany()
            .HasForeignKey(e => e.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
