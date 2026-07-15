using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectVersionPlanningParameterConfiguration : IEntityTypeConfiguration<ProjectVersionPlanningParameter>
{
    public void Configure(EntityTypeBuilder<ProjectVersionPlanningParameter> builder)
    {
        builder.ToTable("ProjectVersionPlanningParameters");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ZPessimistic).HasPrecision(18, 4);
        builder.Property(p => p.ZBase).HasPrecision(18, 4);
        builder.Property(p => p.ZOptimistic).HasPrecision(18, 4);
        builder.Property(p => p.ZActual).HasPrecision(18, 4);

        builder.HasIndex(p => new { p.ProjectAnalysisVersionId, p.IicWorkDirectionId }).IsUnique();

        builder.HasOne(p => p.ProjectAnalysisVersion)
            .WithMany()
            .HasForeignKey(p => p.ProjectAnalysisVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.IicWorkDirection)
            .WithMany()
            .HasForeignKey(p => p.IicWorkDirectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
