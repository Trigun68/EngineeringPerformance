using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectCharacteristicOptionConfiguration : IEntityTypeConfiguration<ProjectCharacteristicOption>
{
    public void Configure(EntityTypeBuilder<ProjectCharacteristicOption> builder)
    {
        builder.ToTable("ProjectCharacteristicOptions");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.ValuePercent)
            .HasPrecision(5, 2);

        builder.HasIndex(o => o.CharacteristicId);
    }
}
