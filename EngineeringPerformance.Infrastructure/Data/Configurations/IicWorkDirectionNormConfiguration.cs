using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EngineeringPerformance.Infrastructure.Data.Configurations;

public sealed class IicWorkDirectionNormConfiguration : IEntityTypeConfiguration<IicWorkDirectionNorm>
{
    public void Configure(EntityTypeBuilder<IicWorkDirectionNorm> builder)
    {
        builder.ToTable("IicWorkDirectionNorms");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Performance)
            .HasPrecision(18, 4);

        builder.Property(n => n.Estimate)
            .HasPrecision(18, 4);

        builder.Property(n => n.StandardDeviation)
            .HasPrecision(18, 4);

        builder.HasOne(n => n.WorkDirection)
            .WithMany()
            .HasForeignKey(n => n.WorkDirectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Unit)
            .WithMany(u => u.Norms)
            .HasForeignKey(n => n.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        // V2-A: одна норма на направление в заданной временной точке.
        // Ранее при накладывающихся окнах сервисы Performance и Forecast выбирали
        // норму недетерминированно (First/OrderByDescending First), что давало
        // расхождение Z-score на тех же данных.
        builder.HasIndex(n => new { n.WorkDirectionId, n.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom");

        // V2-A: «закрытие» окна нормы не может быть раньше её открытия.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_IicWorkDirectionNorms_EffectiveRange",
            "[EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]"));
    }
}
