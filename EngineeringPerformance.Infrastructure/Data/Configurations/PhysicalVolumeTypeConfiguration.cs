using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class PhysicalVolumeTypeConfiguration : IEntityTypeConfiguration<PhysicalVolumeType>
{
    public void Configure(EntityTypeBuilder<PhysicalVolumeType> builder)
    {
        builder.ToTable("PhysicalVolumeTypes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Code)
            .HasMaxLength(50);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);
    }
}
