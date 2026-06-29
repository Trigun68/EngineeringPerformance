using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class IicWorkDirectionConfiguration : IEntityTypeConfiguration<IicWorkDirection>
{
    public void Configure(EntityTypeBuilder<IicWorkDirection> builder)
    {
        builder.ToTable("IicWorkDirections");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.HasOne(d => d.Parent)
            .WithMany(d => d.Children)
            .HasForeignKey(d => d.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Unit)
            .WithMany(u => u.WorkDirections)
            .HasForeignKey(d => d.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.HasIndex(d => d.UnitId);
        builder.HasIndex(d => d.ParentId);
        builder.HasIndex(d => d.IsArchived);
    }
}
