using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class DocSetConfiguration : IEntityTypeConfiguration<DocSet>
{
    public void Configure(EntityTypeBuilder<DocSet> builder)
    {
        builder.ToTable("DocSets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UQ_DocSets_Code");
    }
}
