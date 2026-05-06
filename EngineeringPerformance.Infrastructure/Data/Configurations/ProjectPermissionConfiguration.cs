using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class ProjectPermissionConfiguration : IEntityTypeConfiguration<ProjectPermission>
{
    public void Configure(EntityTypeBuilder<ProjectPermission> builder)
    {
        builder.ToTable("ProjectPermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Level)
            .HasConversion<int>()
            .HasDefaultValue(EngineeringPerformance.Core.Enums.AccessLevel.Read);

        // Один пользователь — одна запись на проект.
        builder.HasIndex(p => new { p.ProjectId, p.UserId })
            .IsUnique()
            .HasDatabaseName("UX_ProjectPermissions_Project_User");

        builder.HasOne(p => p.Project)
            .WithMany(p => p.Permissions)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.UserId);
    }
}
