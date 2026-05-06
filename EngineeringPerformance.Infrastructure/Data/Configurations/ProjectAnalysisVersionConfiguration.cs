using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectAnalysisVersionConfiguration : IEntityTypeConfiguration<ProjectAnalysisVersion>
{
    public void Configure(EntityTypeBuilder<ProjectAnalysisVersion> builder)
    {
        builder.ToTable("ProjectAnalysisVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(2000);

        builder.Property(v => v.Direction)
            .HasConversion<int>();

        builder.HasOne(v => v.PhysicalVolumeType)
            .WithMany()
            .HasForeignKey(v => v.PhysicalVolumeTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.PhysicalVolumes)
            .WithOne(pv => pv.ProjectAnalysisVersion)
            .HasForeignKey(pv => pv.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.LaborIntensities)
            .WithOne(li => li.ProjectAnalysisVersion)
            .HasForeignKey(li => li.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
