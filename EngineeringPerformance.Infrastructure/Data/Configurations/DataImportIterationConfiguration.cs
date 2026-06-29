using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class DataImportIterationConfiguration : IEntityTypeConfiguration<DataImportIteration>
{
    public void Configure(EntityTypeBuilder<DataImportIteration> builder)
    {
        builder.ToTable("DataImportIterations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.DataType)
            .HasConversion<int>();

        builder.Property(i => i.Description)
            .HasMaxLength(500);

        builder.Property(i => i.SourceFileName)
            .HasMaxLength(500);

        builder.Property(i => i.SourceFileHash)
            .HasMaxLength(64)
            .IsFixedLength();

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProjectAnalysisVersion)
            .WithMany(v => v.ImportIterations)
            .HasForeignKey(i => i.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ImportedByUser)
            .WithMany()
            .HasForeignKey(i => i.ImportedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.ProjectAnalysisVersionId);
        builder.HasIndex(i => new { i.ProjectAnalysisVersionId, i.DataType });
        builder.HasIndex(i => i.SourceFileHash)
            .HasFilter("SourceFileHash IS NOT NULL");
        builder.HasIndex(i => i.ImportedByUserId);
    }
}
