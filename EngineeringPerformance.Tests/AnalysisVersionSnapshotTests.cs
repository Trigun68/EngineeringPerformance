using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

/// <summary>
/// V2-B.1 регрессии: PerformanceService и ProjectForecastService обязаны читать нормы из
/// AnalysisVersionSnapshots (зафиксированных на момент создания версии), а не из текущего
/// справочника IicWorkDirectionNorms. Это гарантирует, что появление новой нормы или закрытие
/// старой после создания версии не «двигает» Z-score и сценарии в исторических отчётах.
/// </summary>
public sealed class AnalysisVersionSnapshotTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task CreateAsync_PopulatesSnapshotForActiveNorms()
    {
        var factory = CreateFactory();

        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "direction-A", Code = "A" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 11, Name = "direction-B", Code = "B" });
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 1.0m,
                Estimate = 0m,
                StandardDeviation = 0.2m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-30),
                EffectiveTo = null
            });
            // У direction-B нормы нет — снимок должен её пропустить, а не упасть.
            await seed.SaveChangesAsync();
        }

        var service = new ProjectAnalysisVersionDirectoryService(factory);
        await service.CreateAsync(new ProjectAnalysisVersion
        {
            ProjectId = 1,
            VersionName = "v1",
            Direction = AnalysisDirection.Fact
        });

        await using var ctx = factory.CreateDbContext();
        var snapshots = await ctx.AnalysisVersionSnapshots.AsNoTracking().ToListAsync();

        snapshots.Should().HaveCount(1, "только direction-A имеет действующую норму");
        snapshots[0].WorkDirectionId.Should().Be(10);
        snapshots[0].NormId.Should().Be(100);
    }

    [Fact]
    public async Task CreateAsync_PicksMostRecentEffectiveFrom_WhenOverlappingNormsExist()
    {
        var factory = CreateFactory();
        var now = DateTime.UtcNow;

        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "dir", Code = "A" });
            // Грязные данные: две нормы для одного направления, чьи окна перекрыты.
            // Без детерминированной выборки .First() в LINQ выбирал бы случайно и тихо ломал отчёт.
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 1.0m,
                Estimate = 0m,
                StandardDeviation = 0.2m,
                EffectiveFrom = now.AddDays(-30),
                EffectiveTo = null
            });
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 101,
                WorkDirectionId = 10,
                Performance = 5.0m,
                Estimate = 1.609m,
                StandardDeviation = 0.2m,
                EffectiveFrom = now.AddDays(-1),
                EffectiveTo = null
            });
            await seed.SaveChangesAsync();
        }

        var service = new ProjectAnalysisVersionDirectoryService(factory);
        await service.CreateAsync(new ProjectAnalysisVersion
        {
            ProjectId = 1,
            VersionName = "v1",
            Direction = AnalysisDirection.Fact
        });

        await using var ctx = factory.CreateDbContext();
        var snapshot = await ctx.AnalysisVersionSnapshots.AsNoTracking().SingleAsync();
        snapshot.NormId.Should().Be(101, "выбираться должна норма с самой свежей EffectiveFrom");
    }

    [Fact]
    public async Task PerformanceService_UsesSnapshot_NotLatestNormAddedAfterVersion()
    {
        var factory = CreateFactory();
        var creation = DateTime.UtcNow.AddDays(-7);

        // Сценарий: версия создана неделю назад с нормой A (Performance=2);
        // позже норму закрыли (EffectiveTo=сегодня) и добавили B (Performance=10, EffectiveFrom=сегодня).
        // Без снимка PerformanceService runtime-фильтром выбрал бы B и Z-score исторического
        // отчёта изменился бы. Со снимком должна остаться норма A.
        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "dir", Code = "A" });

            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 2.0m,
                Estimate = 0.693m,
                StandardDeviation = 0.5m,
                EffectiveFrom = creation.AddDays(-30),
                EffectiveTo = null
            });

            seed.PhysicalVolumes.Add(new PhysicalVolume
            {
                Id = 50,
                Code = "PV-1",
                Name = "pv",
                ClassifierId = 10,
                Coefficient = 1m,
                VolumeTypeId = 1,
                IsAssessment = true
            });
            await seed.SaveChangesAsync();

            var version = new ProjectAnalysisVersion
            {
                Id = 1,
                ProjectId = 1,
                VersionName = "v1",
                CreatedAt = creation,
                Direction = AnalysisDirection.Fact
            };
            seed.ProjectAnalysisVersions.Add(version);
            await seed.SaveChangesAsync();

            // Снимок симулирует то, что сейчас делает CreateAsync: NormId=100 на момент creation.
            seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 10,
                NormId = 100
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
                WorkDirectionId = 10,
                ActualHours = 200m,
                PlannedHours = 200m
            });

            // После создания версии норму A закрывают и добавляют B с другим Performance.
            // SaveChangesAsync до .Local.First — без него DbSet ещё не видит вставленный ряд
            // в InMemory-провайдере, и .First падает «Sequence contains no elements».
            await seed.SaveChangesAsync();
            seed.IicWorkDirectionNorms.Local.First(n => n.Id == 100).EffectiveTo = DateTime.UtcNow;
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 101,
                WorkDirectionId = 10,
                Performance = 10.0m,
                Estimate = 2.302m,
                StandardDeviation = 0.5m,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveTo = null
            });
            await seed.SaveChangesAsync();
        }

        var perf = new PerformanceService(factory);
        var rows = await perf.GetPerformanceDataAsync(projectId: 1, versionId: 1);

        var row = rows.Single(r => r.WorkDirectionId == 10);
        row.NormProductivity.Should().Be(2.0m, "снимок зафиксировал норму A — норма B не должна влиять на исторический отчёт");
    }

    [Fact]
    public async Task ProjectForecastService_UsesSnapshot_NotLatestNorm()
    {
        var factory = CreateFactory();
        var creation = DateTime.UtcNow.AddDays(-7);

        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "dir", Code = "A" });
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 2.0m,
                Estimate = 0.693m,
                StandardDeviation = 0.3m,
                EffectiveFrom = creation.AddDays(-30),
                EffectiveTo = null
            });
            seed.PhysicalVolumes.Add(new PhysicalVolume
            {
                Id = 50,
                Code = "PV-1",
                Name = "pv",
                ClassifierId = 10,
                Coefficient = 1m,
                VolumeTypeId = 1,
                IsAssessment = true
            });
            seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
            {
                Id = 1,
                ProjectId = 1,
                VersionName = "v1",
                CreatedAt = creation,
                Direction = AnalysisDirection.Fact
            });
            seed.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 10,
                NormId = 100
            });
            seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
            {
                Id = 200,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                PhysicalVolumeId = 50,
                Quantity = 50m
            });
            seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity
            {
                Id = 300,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                WorkDirectionId = 10,
                PlannedHours = 100m,
                ActualHours = 0m
            });

            await seed.SaveChangesAsync();
            seed.IicWorkDirectionNorms.Local.First(n => n.Id == 100).EffectiveTo = DateTime.UtcNow;
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 101,
                WorkDirectionId = 10,
                Performance = 10.0m,
                Estimate = 2.302m,
                StandardDeviation = 0.3m,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveTo = null
            });
            await seed.SaveChangesAsync();
        }

        var forecast = new ProjectForecastService(factory);
        var report = await forecast.GenerateForecastAsync(projectId: 1, versionId: 1);

        var row = report.PlannedVolumes.Single(r => r.WorkDirectionId == 10);
        row.NormProductivity.Should().Be(2.0m, "forecast тоже обязан читать норму из снимка, а не свежую");
    }
}
