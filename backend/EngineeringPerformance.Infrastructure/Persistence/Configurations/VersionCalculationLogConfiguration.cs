using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class VersionCalculationLogConfiguration : IEntityTypeConfiguration<VersionCalculationLog>
{
    public void Configure(EntityTypeBuilder<VersionCalculationLog> builder)
    {
        builder.ToTable("VersionCalculationLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Trigger).HasColumnName("Trigger").IsRequired().HasMaxLength(64);
        builder.Property(x => x.BaseHoursSum).HasPrecision(18, 4);
        builder.Property(x => x.OptimisticHoursSum).HasPrecision(18, 4);
        builder.Property(x => x.PessimisticHoursSum).HasPrecision(18, 4);

        builder.HasOne(x => x.ProjectAnalysisVersion)
            .WithMany()
            .HasForeignKey(x => x.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProjectAnalysisVersionId, x.Utc })
            .HasDatabaseName("IX_VersionCalculationLogs_Version_Utc");
    }
}
