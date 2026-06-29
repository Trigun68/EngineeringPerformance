using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Регрессионные тесты для <see cref="PerformanceService"/>:
/// контракт пустой БД + средне-взвешенная фактическая оценка по поддереву (since 2026-05-05).
/// </summary>
public sealed class PerformanceServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task GetPerformanceData_EmptyDatabase_ReturnsPlaceholderOnly()
    {
        var factory = CreateFactory();
        var service = new PerformanceService(factory);

        var result = await service.GetPerformanceDataAsync(projectId: 1, versionId: 1);

        result.Should().NotBeNull();
        // На пустой БД сервис возвращает синтетическую строку-плейсхолдер «(пусто)»,
        // а не пустую коллекцию (ожидание UI: всегда есть хотя бы одна строка).
        result.Should().ContainSingle();
        result[0].WorkDirectionName.Should().Be("(пусто)");
    }

    /// <summary>
    /// Корневое направление без собственных часов/объёмов должно получать
    /// средне-взвешенную фактическую оценку по часам поддерева:
    ///   leaf-A: hours=200, productivity=2 → estimate=ln(2)
    ///   leaf-B: hours=150, productivity=3 → estimate=ln(3)
    ///   parent: weighted = (ln(2)*200 + ln(3)*150) / (200+150)
    /// До правки 2026-05-05 родитель оставался null, теперь — агрегат по поддереву.
    /// </summary>
    [Fact]
    public async Task GetPerformanceData_ParentWithoutOwnFact_AggregatesWeightedEstimateFromChildren()
    {
        var factory = CreateFactory();
        await SeedHierarchyAsync(factory);

        var service = new PerformanceService(factory);
        var rows = await service.GetPerformanceDataAsync(projectId: 1, versionId: 1);

        var parent = rows.Single(r => r.WorkDirectionId == 10);
        var leafA = rows.Single(r => r.WorkDirectionId == 11);
        var leafB = rows.Single(r => r.WorkDirectionId == 12);

        leafA.FactEstimate.Should().BeApproximately((decimal)Math.Log(2.0), 1e-6m);
        leafB.FactEstimate.Should().BeApproximately((decimal)Math.Log(3.0), 1e-6m);

        var expected = ((decimal)Math.Log(2.0) * 200m + (decimal)Math.Log(3.0) * 150m) / 350m;
        parent.FactEstimate.Should().NotBeNull();
        parent.FactEstimate!.Value.Should().BeApproximately(expected, 1e-6m);

        // Pre-order: родитель идёт раньше детей в результирующем списке.
        var indices = rows.Select((r, i) => (r.WorkDirectionId, i)).ToDictionary(x => x.WorkDirectionId, x => x.i);
        indices[10].Should().BeLessThan(indices[11]);
        indices[10].Should().BeLessThan(indices[12]);
    }

    [Fact]
    public async Task InvalidateWorkDirectionHierarchyCache_ReloadsIicTreeFromDatabase()
    {
        var factory = CreateFactory();
        await SeedHierarchyAsync(factory);

        var service = new PerformanceService(factory);
        await service.GetPerformanceDataAsync(1, 1);

        await using (var ctx = await factory.CreateDbContextAsync())
        {
            ctx.IicWorkDirections.Add(new IicWorkDirection { Id = 13, Name = "leaf-C", Code = "C", ParentId = 10 });
            await ctx.SaveChangesAsync();
        }

        var beforeInvalidate = await service.GetPerformanceDataAsync(1, 1);
        beforeInvalidate.Should().NotContain(r => r.WorkDirectionId == 13);

        service.InvalidateWorkDirectionHierarchyCache();

        var afterInvalidate = await service.GetPerformanceDataAsync(1, 1);
        afterInvalidate.Should().Contain(r => r.WorkDirectionId == 13);
    }

    /// <summary>
    /// Z-score родительского узла должен пересчитываться по агрегированной FactEstimate,
    /// а NormEstimate/NormStdDev брать строго из снимка норм (никаких вычислений сверху).
    /// </summary>
    [Fact]
    public async Task GetPerformanceData_ParentZScore_ComputedFromAggregatedFactEstimate()
    {
        var factory = CreateFactory();
        await SeedHierarchyAsync(factory);

        var service = new PerformanceService(factory);
        var rows = await service.GetPerformanceDataAsync(projectId: 1, versionId: 1);

        var parent = rows.Single(r => r.WorkDirectionId == 10);

        parent.NormEstimate.Should().Be(1.0m, "норма берётся из снимка как есть");
        parent.NormStdDev.Should().Be(0.5m);

        var expectedAgg = ((decimal)Math.Log(2.0) * 200m + (decimal)Math.Log(3.0) * 150m) / 350m;
        var expectedZ = (expectedAgg - 1.0m) / 0.5m;
        parent.ZScore.Should().NotBeNull();
        parent.ZScore!.Value.Should().BeApproximately(expectedZ, 1e-6m);
    }

    private static async Task SeedHierarchyAsync(IDbContextFactory<AppDbContext> factory)
    {
        await using var seed = factory.CreateDbContext();

        seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });

        // Иерархия IIC: один корень + два листа.
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 11, Name = "leaf-A", Code = "A", ParentId = 10 });
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 12, Name = "leaf-B", Code = "B", ParentId = 10 });

        // Норма только на корень — проверяем, что родительский Z-score берёт NormEstimate
        // напрямую из снимка, а FactEstimate агрегирует поддерево.
        seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
        {
            Id = 100,
            WorkDirectionId = 10,
            Performance = 2.5m,
            Estimate = 1.0m,
            StandardDeviation = 0.5m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = null
        });

        // Каталог физ. объёмов — по одному на каждый лист (через ClassifierId).
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 50, Code = "PV-A", Name = "pv-a", ClassifierId = 11, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 51, Code = "PV-B", Name = "pv-b", ClassifierId = 12, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });

        seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 1,
            ProjectId = 1,
            VersionName = "v1",
            CreatedAt = DateTime.UtcNow,
            Direction = AnalysisDirection.Fact
        });

        await seed.SaveChangesAsync();

        // Снимок — нормативный набор для версии (источник норм для PerformanceService).
        seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
        {
            ProjectAnalysisVersionId = 1,
            WorkDirectionId = 10,
            NormId = 100
        });

        // Объёмы и часы по проекту:
        //   leaf-A: 100 ед. × productivity 2 = 200 часов
        //   leaf-B:  50 ед. × productivity 3 = 150 часов
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 200, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 50, Quantity = 100m });
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 201, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 51, Quantity = 50m });

        seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity { Id = 300, ProjectId = 1, ProjectAnalysisVersionId = 1, WorkDirectionId = 11, ActualHours = 200m, PlannedHours = 200m });
        seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity { Id = 301, ProjectId = 1, ProjectAnalysisVersionId = 1, WorkDirectionId = 12, ActualHours = 150m, PlannedHours = 150m });

        await seed.SaveChangesAsync();
    }

    /// <summary>
    /// В справочнике могут существовать несколько норм с одним WorkDirectionId;
    /// ключ словаря норм для версии — WorkDirectionId из снимка, а не из Norm.
    /// </summary>
    [Fact]
    public async Task GetPerformanceData_DuplicateNormWorkDirectionIdsInCatalog_DoesNotThrow()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 11, Name = "leaf", Code = "A", ParentId = 10 });

            // Две нормы с одним WorkDirectionId (исторический дубликат в справочнике).
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 2.0m,
                Estimate = 1.0m,
                StandardDeviation = 0.5m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-60),
                EffectiveTo = DateTime.UtcNow.AddDays(-1)
            });
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 101,
                WorkDirectionId = 10,
                Performance = 2.5m,
                Estimate = 1.2m,
                StandardDeviation = 0.6m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-30),
                EffectiveTo = null
            });

            seed.PhysicalVolumes.Add(new PhysicalVolume
            {
                Id = 50,
                Code = "PV-A",
                Name = "pv-a",
                ClassifierId = 11,
                Coefficient = 1m,
                VolumeTypeId = 1,
                IsAssessment = true
            });

            seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
            {
                Id = 1,
                ProjectId = 1,
                VersionName = "v1",
                CreatedAt = DateTime.UtcNow,
                Direction = AnalysisDirection.Fact
            });

            await seed.SaveChangesAsync();

            seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 10,
                NormId = 100
            });
            seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 11,
                NormId = 101
            });

            seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
            {
                Id = 200,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                PhysicalVolumeId = 50,
                Quantity = 100m
            });
            seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity
            {
                Id = 300,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 11,
                ActualHours = 200m,
                PlannedHours = 200m
            });

            await seed.SaveChangesAsync();
        }

        var service = new PerformanceService(factory);
        var act = async () => await service.GetPerformanceDataAsync(projectId: 1, versionId: 1);

        var rows = await act.Should().NotThrowAsync();
        rows.Subject.Should().Contain(r => r.WorkDirectionId == 10);
        rows.Subject.Should().Contain(r => r.WorkDirectionId == 11);
    }
}
