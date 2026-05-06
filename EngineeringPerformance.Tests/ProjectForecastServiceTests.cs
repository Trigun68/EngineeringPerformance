using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

public sealed class ProjectForecastServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContextFactory(options);
    }

    [Fact]
    public async Task GenerateForecast_VersionNotFound_Throws()
    {
        var factory = CreateFactory();
        var service = new ProjectForecastService(factory);

        var act = () => service.GenerateForecastAsync(projectId: 1, versionId: 999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GenerateForecast_VersionExistsWithoutData_DoesNotThrow()
    {
        var factory = CreateFactory();
        await using var seed = factory.CreateDbContext();
        seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
        seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 1,
            ProjectId = 1,
            Name = "v1",
            CreatedAt = DateTime.UtcNow
        });
        await seed.SaveChangesAsync();

        var service = new ProjectForecastService(factory);

        var act = () => service.GenerateForecastAsync(projectId: 1, versionId: 1);

        // Не должен падать на пустых ProjectLaborIntensities/PhysicalVolumes —
        // forecast формируется и возвращает пустые сценарии.
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Алгоритм прогноза (линейный, с 2026-05-06):
    ///   Базовый       (20%): ФО × Performance
    ///   Оптимистичный (15%): ФО × (Performance − 0.5 × StdDev)
    ///   Пессимистичный(15%): ФО × (Performance + 0.5 × StdDev)
    ///
    /// Фикстура: ФО=1, Performance=2, StdDev=1. Ожидаем:
    ///   base = 1 × 2         = 2.00
    ///   opt  = 1 × (2 − 0.5) = 1.50
    ///   pes  = 1 × (2 + 0.5) = 2.50
    /// </summary>
    [Fact]
    public async Task GenerateForecast_ScenarioHours_FollowNormEstimateWithHalfStdDeviation()
    {
        var factory = CreateFactory();

        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });

            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 2m,
                Estimate = (decimal)Math.Log(2.0),
                StandardDeviation = 1m,
                EffectiveFrom = DateTime.UtcNow.AddDays(-30),
                EffectiveTo = null
            });
            seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 50, Code = "PV", Name = "pv", ClassifierId = 10, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
            seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
            {
                Id = 1,
                ProjectId = 1,
                Name = "v1",
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
            seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
            {
                Id = 200,
                ProjectId = 1,
                ProjectAnalysisVersionId = 1,
                PhysicalVolumeId = 50,
                Quantity = 1m
            });
            await seed.SaveChangesAsync();
        }

        var service = new ProjectForecastService(factory);
        var report = await service.GenerateForecastAsync(projectId: 1, versionId: 1);

        var baseScenario = report.Scenarios.Single(s => s.ScenarioName == "Базовый");
        var optimistic = report.Scenarios.Single(s => s.ScenarioName == "Оптимистичный");
        var pessimistic = report.Scenarios.Single(s => s.ScenarioName == "Пессимистичный");

        // base = 1 × 2 = 2
        baseScenario.TotalLaborHours.Should().BeApproximately(2m, 0.01m);
        // opt  = 1 × (2 − 0.5×1) = 1.5
        optimistic.TotalLaborHours.Should().BeApproximately(1.5m, 0.01m);
        // pes  = 1 × (2 + 0.5×1) = 2.5
        pessimistic.TotalLaborHours.Should().BeApproximately(2.5m, 0.01m);

        optimistic.ZScore.Should().Be(-0.5m);
        baseScenario.ZScore.Should().Be(0m);
        pessimistic.ZScore.Should().Be(0.5m);
    }
}
