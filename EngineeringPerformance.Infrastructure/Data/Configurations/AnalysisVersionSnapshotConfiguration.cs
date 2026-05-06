using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class AnalysisVersionSnapshotConfiguration : IEntityTypeConfiguration<AnalysisVersionSnapshot>
{
    public void Configure(EntityTypeBuilder<AnalysisVersionSnapshot> builder)
    {
        builder.ToTable("AnalysisVersionSnapshots");

        builder.HasKey(s => s.Id);

        // Одна запись на (версия × направление). Гарантирует, что при расчёте
        // Z-score сервис всегда находит ровно одну норму на узел.
        builder.HasIndex(s => new { s.ProjectAnalysisVersionId, s.WorkDirectionId })
            .IsUnique()
            .HasDatabaseName("UX_AnalysisVersionSnapshots_Version_WorkDirection");

        builder.HasOne(s => s.ProjectAnalysisVersion)
            .WithMany()
            .HasForeignKey(s => s.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // WorkDirection и Norm — Restrict, чтобы случайное удаление направления
        // или нормы не оставило снимок «висящим» и не отвалилось каскадом.
        builder.HasOne(s => s.WorkDirection)
            .WithMany()
            .HasForeignKey(s => s.WorkDirectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Norm)
            .WithMany()
            .HasForeignKey(s => s.NormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.NormId);
    }
}
