using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public class ProjectAnalysisVersionConfiguration : IEntityTypeConfiguration<ProjectAnalysisVersion>
{
    public void Configure(EntityTypeBuilder<ProjectAnalysisVersion> builder)
    {
        builder.ToTable("ProjectAnalysisVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.Property(v => v.VersionName).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Description).HasMaxLength(2000);

        // Primary version is scoped per direction.
        builder.HasIndex(v => new { v.ProjectId, v.Direction, v.IsPrimary })
            .HasDatabaseName("IX_ProjectAnalysisVersions_Primary")
            .IsUnique();

        // Quick lookup by direction.
        builder.HasIndex(v => new { v.ProjectId, v.Direction }).IsUnique(false);

        builder.HasIndex(v => v.SortOrder);
    }
}
