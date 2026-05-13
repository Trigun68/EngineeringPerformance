using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectLaborIntensityConfiguration : IEntityTypeConfiguration<ProjectLaborIntensity>
{
    public void Configure(EntityTypeBuilder<ProjectLaborIntensity> builder)
    {
        builder.ToTable("ProjectLaborIntensities");

        builder.HasKey(li => li.Id);

        // Должно совпадать с миграцией ExpandLaborDocumentationCodeTo450 (импорт TF, длинные составные коды).
        builder.Property(li => li.DocumentationCode)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(li => li.PlannedHours)
            .HasPrecision(18, 4);

        builder.Property(li => li.ActualHours)
            .HasPrecision(18, 4);

        builder.Property(li => li.Notes)
            .HasMaxLength(2000);

        builder.Property(li => li.Brand)
            .HasMaxLength(100);

        builder.HasOne(li => li.Project)
            .WithMany()
            .HasForeignKey(li => li.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.ProjectAnalysisVersion)
            .WithMany(v => v.LaborIntensities)
            .HasForeignKey(li => li.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(li => li.WorkDirection)
            .WithMany()
            .HasForeignKey(li => li.WorkDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.LaborIntensity)
            .WithMany()
            .HasForeignKey(li => li.LaborIntensityId)
            .OnDelete(DeleteBehavior.SetNull);

        // V2-D: явный FK на PhysicalVolume с ON DELETE SET NULL.
        // Без явной конфигурации EF создавал shadow-свойство и оставлял
        // delete-rule = NO_ACTION (ClientSetNull) — тогда удаление PhysicalVolume
        // падало бы с FK-conflict, а не сохраняло историю с обнулённой привязкой.
        builder.HasOne(li => li.PhysicalVolume)
            .WithMany(pv => pv.LaborIntensities)
            .HasForeignKey(li => li.PhysicalVolumeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(li => li.Brand);

        builder.HasIndex(li => li.LaborIntensityId);

        builder.HasIndex(li => li.PhysicalVolumeId);

        builder.HasIndex(li => new { li.ProjectAnalysisVersionId, li.ProjectId, li.DocumentationCode, li.WorkDirectionId })
            .IsUnique()
            .HasFilter("[WorkDirectionId] IS NOT NULL");
    }
}
