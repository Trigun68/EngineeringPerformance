using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class AccessRightConfiguration : IEntityTypeConfiguration<AccessRight>
{
    public void Configure(EntityTypeBuilder<AccessRight> builder)
    {
        builder.ToTable("AccessRights");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ObjectName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(r => r.Role)
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.RoleId, r.ObjectName })
            .IsUnique();
    }
}
