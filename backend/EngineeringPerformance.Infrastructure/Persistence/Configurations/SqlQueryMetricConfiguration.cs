using EngineeringPerformance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Persistence.Configurations;

public sealed class SqlQueryMetricConfiguration : IEntityTypeConfiguration<SqlQueryMetric>
{
    public void Configure(EntityTypeBuilder<SqlQueryMetric> builder)
    {
        builder.ToTable("SqlQueryMetrics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.TimestampUtc).IsRequired();
        builder.Property(x => x.Endpoint).IsRequired().HasMaxLength(512);
        builder.Property(x => x.RequestPath).HasMaxLength(512);
        builder.Property(x => x.QueryType).IsRequired().HasMaxLength(16);
        builder.Property(x => x.TablesInvolved).HasMaxLength(1000);
        builder.Property(x => x.DurationMs).HasPrecision(18, 4);
        builder.Property(x => x.RequestDurationMs).HasPrecision(18, 4);
        builder.Property(x => x.QueryHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.SqlPreview).HasMaxLength(2000);
        builder.Property(x => x.ParametersJson).HasMaxLength(4000);
        builder.Property(x => x.RequestParametersJson).HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => x.TimestampUtc)
            .HasDatabaseName("IX_SqlQueryMetrics_TimestampUtc");
        builder.HasIndex(x => new { x.QueryHash, x.TimestampUtc })
            .HasDatabaseName("IX_SqlQueryMetrics_QueryHash_Timestamp");
        builder.HasIndex(x => new { x.Endpoint, x.TimestampUtc })
            .HasDatabaseName("IX_SqlQueryMetrics_Endpoint_Timestamp");
        builder.HasIndex(x => new { x.QueryType, x.TimestampUtc })
            .HasDatabaseName("IX_SqlQueryMetrics_QueryType_Timestamp");
        builder.HasIndex(x => x.DurationMs)
            .HasDatabaseName("IX_SqlQueryMetrics_DurationMs");
    }
}
