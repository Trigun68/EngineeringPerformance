using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class PhysicalVolumeConfiguration : IEntityTypeConfiguration<PhysicalVolume>
{
    public void Configure(EntityTypeBuilder<PhysicalVolume> builder)
    {
        builder.ToTable("PhysicalVolumes");

        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pv => pv.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pv => pv.Description)
            .HasMaxLength(4000);

        builder.Property(pv => pv.DocumentSet)
            .HasMaxLength(100);

        builder.Property(pv => pv.Coefficient)
            .HasPrecision(18, 4)
            .HasDefaultValue(1m);

        builder.HasIndex(pv => pv.Code);
        builder.HasIndex(pv => pv.VolumeTypeId);
        builder.HasIndex(pv => pv.ClassifierId);
        builder.HasIndex(pv => pv.ParentId);
        builder.HasIndex(pv => pv.IsArchived);
        builder.HasIndex(pv => pv.IsAssessment);
        builder.HasIndex(pv => pv.DocumentSet);

        // Композитные индексы под основные сценарии чтения справочника.
        // (IsArchived, VolumeTypeId) — фильтр сетки по виду + неархивные.
        // (IsArchived, ParentId, SortOrder) — построение дерева в порядке сортировки.
        // (IsArchived, SortOrder, Name) — покрывает ORDER BY основной выборки грида.
        builder.HasIndex(pv => new { pv.IsArchived, pv.VolumeTypeId });
        builder.HasIndex(pv => new { pv.IsArchived, pv.ParentId, pv.SortOrder });
        builder.HasIndex(pv => new { pv.IsArchived, pv.SortOrder, pv.Name });

        builder.HasOne(pv => pv.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(pv => pv.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.VolumeType)
            .WithMany()
            .HasForeignKey(pv => pv.VolumeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pv => pv.Classifier)
            .WithMany()
            .HasForeignKey(pv => pv.ClassifierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(pv => pv.Unit)
            .WithMany()
            .HasForeignKey(pv => pv.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(pv => pv.UnitId);
    }
}
