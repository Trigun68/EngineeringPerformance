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
    /// Регрессия (since 2026-05-05): сценарии строятся из нормативной производительности,
    /// оптимистичный/пессимистичный = экспонента от NormEstimate ∓ 0.5·NormStdDev
    /// (std — в лог-пространстве, т.к. Estimate = ln(Performance)).
    ///
    /// Фикстура: объём=1, NormEstimate=ln(2), std=1. Ожидаем:
    ///   base =  1 × exp(ln 2)                 = 2
    ///   opt  =  1 × exp(ln 2 - 0.5)           ≈ 1.21
    ///   pes  =  1 × exp(ln 2 + 0.5)           ≈ 3.30
    /// </summary>
    [Fact]
    public async Task GenerateForecast_ScenarioHours_FollowNormEstimateWithHalfStdDeviation()
    {
        var factory = CreateFactory();

        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });

            var lnTwo = (decimal)Math.Log(2.0);
            seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
            {
                Id = 100,
                WorkDirectionId = 10,
                Performance = 2m,
                Estimate = lnTwo,
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

        baseScenario.TotalLaborHours.Should().BeApproximately(2m, 0.01m);
        optimistic.TotalLaborHours.Should().BeApproximately((decimal)Math.Exp(Math.Log(2.0) - 0.5), 0.01m);
        pessimistic.TotalLaborHours.Should().BeApproximately((decimal)Math.Exp(Math.Log(2.0) + 0.5), 0.01m);

        // Z-метки сценариев фиксируются константами сервиса: ∓0.5 и 0.
        optimistic.ZScore.Should().Be(-0.5m);
        baseScenario.ZScore.Should().Be(0m);
        pessimistic.ZScore.Should().Be(0.5m);
    }
}
