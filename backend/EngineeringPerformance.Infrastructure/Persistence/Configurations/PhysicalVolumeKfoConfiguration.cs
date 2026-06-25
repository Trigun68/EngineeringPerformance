using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class PhysicalVolumeKfoConfiguration : IEntityTypeConfiguration<PhysicalVolumeKfo>
{
    public void Configure(EntityTypeBuilder<PhysicalVolumeKfo> builder)
    {
        builder.ToTable("PhysicalVolumesKfo");
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Code).IsRequired().HasMaxLength(50);
        builder.Property(pv => pv.Name).IsRequired().HasMaxLength(500);
        builder.Property(pv => pv.Description).HasMaxLength(4000);
        builder.Property(pv => pv.DocumentSet).HasMaxLength(100);
        builder.Property(pv => pv.Coefficient).HasPrecision(18, 4).HasDefaultValue(1m);

        builder.HasIndex(pv => pv.Code);
        builder.HasIndex(pv => pv.ClassifierId);
        builder.HasIndex(pv => pv.ParentId);
        builder.HasIndex(pv => pv.IsArchived);
        builder.HasIndex(pv => pv.IsAssessment);
        builder.HasIndex(pv => pv.DocumentSet);
        builder.HasIndex(pv => pv.UnitId);
        builder.HasIndex(pv => pv.IicNormId);
        builder.HasIndex(pv => new { pv.IsArchived, pv.ParentId, pv.SortOrder });
        builder.HasIndex(pv => new { pv.IsArchived, pv.SortOrder, pv.Name });

        builder.HasOne(pv => pv.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(pv => pv.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.Classifier)
            .WithMany()
            .HasForeignKey(pv => pv.ClassifierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pv => pv.Unit)
            .WithMany()
            .HasForeignKey(pv => pv.UnitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
