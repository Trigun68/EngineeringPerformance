using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class NormSuggestionConfiguration : IEntityTypeConfiguration<NormSuggestion>
{
    public void Configure(EntityTypeBuilder<NormSuggestion> builder)
    {
        builder.ToTable("NormSuggestions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProposedFormulaType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProposedParametersJson).IsRequired();
        builder.Property(x => x.ProjectFilterJson).HasMaxLength(4000);

        builder.HasOne(x => x.Norm)
            .WithMany()
            .HasForeignKey(x => x.NormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.NormId, x.Status });
        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd });
    }
}

public sealed class NormChangeAuditConfiguration : IEntityTypeConfiguration<NormChangeAudit>
{
    public void Configure(EntityTypeBuilder<NormChangeAudit> builder)
    {
        builder.ToTable("NormChangeAudits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChangedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.OldValueJson);
        builder.Property(x => x.NewValueJson);

        builder.HasOne(x => x.Norm)
            .WithMany()
            .HasForeignKey(x => x.NormId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.NormId, x.Timestamp });
    }
}
