using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectCharacteristicConfiguration : IEntityTypeConfiguration<ProjectCharacteristic>
{
    public void Configure(EntityTypeBuilder<ProjectCharacteristic> builder)
    {
        builder.ToTable("ProjectCharacteristics");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Options)
            .WithOne(o => o.Characteristic!)
            .HasForeignKey(o => o.CharacteristicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.IsArchived);
    }
}
