using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class UserActionLogConfiguration : IEntityTypeConfiguration<UserActionLog>
{
    public void Configure(EntityTypeBuilder<UserActionLog> builder)
    {
        builder.ToTable("UserActionLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Timestamp).IsRequired();
        builder.Property(l => l.ActionType).IsRequired().HasMaxLength(64);
        builder.Property(l => l.EntityType).HasMaxLength(100);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.IpOrHost).HasMaxLength(64);

        builder.HasOne(l => l.User)
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.UserId, l.Timestamp })
               .HasDatabaseName("IX_UserActionLogs_UserId_Timestamp");
        builder.HasIndex(l => l.ActionType)
               .HasDatabaseName("IX_UserActionLogs_ActionType");
        builder.HasIndex(l => new { l.EntityType, l.EntityId })
               .HasDatabaseName("IX_UserActionLogs_EntityType_EntityId");
    }
}
