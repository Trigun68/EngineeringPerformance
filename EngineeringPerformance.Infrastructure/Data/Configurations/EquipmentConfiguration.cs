using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Description)
            .HasMaxLength(4000);

        builder.Property(e => e.Coefficient)
            .HasPrecision(18, 4);

        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.IsArchived);
        builder.HasIndex(e => e.ClassifierId);
        builder.HasIndex(e => e.IsAssessment);
        builder.HasIndex(e => new { e.IsArchived, e.ParentId, e.SortOrder });
        builder.HasIndex(e => e.Name);

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Classifier)
            .WithMany()
            .HasForeignKey(e => e.ClassifierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
