using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class ResourceMetricSampleConfiguration : IEntityTypeConfiguration<ResourceMetricSample>
{
    public void Configure(EntityTypeBuilder<ResourceMetricSample> builder)
    {
        builder.ToTable("ResourceMetricSamples");
        builder.HasKey(x => x.TimestampUtc);
        builder.Property(x => x.TimestampUtc).IsRequired();
        builder.Property(x => x.RedisStatus).HasMaxLength(200);
        builder.Property(x => x.SqlStatus).HasMaxLength(200);
    }
}
