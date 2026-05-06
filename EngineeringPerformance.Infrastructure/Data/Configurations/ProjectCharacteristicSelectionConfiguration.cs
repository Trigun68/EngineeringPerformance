using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectCharacteristicSelectionConfiguration : IEntityTypeConfiguration<ProjectCharacteristicSelection>
{
    public void Configure(EntityTypeBuilder<ProjectCharacteristicSelection> builder)
    {
        builder.ToTable("ProjectCharacteristicSelections");

        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Characteristic)
            .WithMany()
            .HasForeignKey(s => s.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Option)
            .WithMany()
            .HasForeignKey(s => s.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Одна характеристика — одно значение на проект.
        builder.HasIndex(s => new { s.ProjectId, s.CharacteristicId }).IsUnique();
        builder.HasIndex(s => s.OptionId);
    }
}
