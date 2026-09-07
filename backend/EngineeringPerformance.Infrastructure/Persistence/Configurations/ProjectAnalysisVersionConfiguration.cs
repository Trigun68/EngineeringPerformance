using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Версия анализа проекта. Конфигурация зеркалит SQL (001, 099, 105, 119, 121):
/// длины строк и индексы совпадают со схемой, чтобы EF-модель не расходилась с БД.
/// RowVersion настраивается в <c>AppDbContext.OnModelCreating</c> (ветка InMemory/SQL).
/// </summary>
public sealed class ProjectAnalysisVersionConfiguration : IEntityTypeConfiguration<ProjectAnalysisVersion>
{
    public const string PrimaryVersionIndexName = "UX_ProjectAnalysisVersions_OnePrimary";

    public void Configure(EntityTypeBuilder<ProjectAnalysisVersion> builder)
    {
        builder.ToTable("ProjectAnalysisVersions");

        builder.HasKey(v => v.Id);

        // 001_InitialSchema: VersionName nvarchar(200); Name — nvarchar(max) (без ограничения).
        builder.Property(v => v.VersionName).IsRequired().HasMaxLength(200);
        // 099_WebApiSchemaPatch: Description nvarchar(2000).
        builder.Property(v => v.Description).HasMaxLength(2000);

        builder.HasIndex(v => v.ProjectId)
            .HasDatabaseName("IX_ProjectAnalysisVersions_ProjectId");

        builder.HasIndex(v => v.PhysicalVolumeTypeId)
            .HasDatabaseName("IX_ProjectAnalysisVersions_PhysicalVolumeTypeId");

        // Одна основная (IsPrimary) неархивная версия на проект и направление (Plan/Fact).
        // Filtered unique: не-primary и архивные версии не ограничиваются (121_...).
        builder.HasIndex(v => new { v.ProjectId, v.Direction })
            .HasDatabaseName(PrimaryVersionIndexName)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1 AND [IsArchived] = 0");
    }
}
