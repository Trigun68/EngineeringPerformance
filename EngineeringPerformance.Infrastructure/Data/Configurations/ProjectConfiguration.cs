using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.WorkNumber)
            .HasMaxLength(100);

        builder.Property(p => p.ContractNumber)
            .HasMaxLength(200);

        builder.Property(p => p.CalendarPlanNumber)
            .HasMaxLength(200);

        builder.Property(p => p.Type)
            .HasMaxLength(200);

        builder.Property(p => p.Customer)
            .HasMaxLength(500);

        builder.Property(p => p.Contractor)
            .HasMaxLength(500);

        builder.Property(p => p.Gip)
            .HasMaxLength(300);

        builder.Property(p => p.WorkStatus)
            .HasMaxLength(100);

        builder.Property(p => p.WorkStart)
            .HasColumnType("date");

        builder.Property(p => p.WorkEnd)
            .HasColumnType("date");

        builder.Property(p => p.ContractDate)
            .HasColumnType("date");

        builder.Property(p => p.Program)
            .HasMaxLength(300);

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.HasMany(p => p.AnalysisVersions)
            .WithOne(v => v.Project)
            .HasForeignKey(v => v.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
