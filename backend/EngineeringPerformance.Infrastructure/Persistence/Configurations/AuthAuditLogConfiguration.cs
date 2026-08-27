using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class AuthAuditLogConfiguration : IEntityTypeConfiguration<AuthAuditLog>
{
    public void Configure(EntityTypeBuilder<AuthAuditLog> builder)
    {
        builder.ToTable("AuthAuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Timestamp).IsRequired();
        builder.Property(log => log.EventType).IsRequired();
        builder.Property(log => log.Identifier).HasMaxLength(256);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.Property(log => log.UserAgent).HasMaxLength(1024);
        builder.Property(log => log.CorrelationId).HasMaxLength(128);
        builder.Property(log => log.Details).HasMaxLength(4000);

        builder.HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(log => log.Timestamp);
        builder.HasIndex(log => new { log.UserId, log.Timestamp });
        builder.HasIndex(log => new { log.EventType, log.Timestamp });
    }
}
