using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.WindowsUsername).HasMaxLength(100);
        builder.Property(u => u.WindowsSid).HasMaxLength(64);

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasIndex(u => u.WindowsUsername)
            .IsUnique()
            .HasFilter("WindowsUsername IS NOT NULL");

        builder.HasIndex(u => u.WindowsSid)
            .IsUnique()
            .HasFilter("WindowsSid IS NOT NULL");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
