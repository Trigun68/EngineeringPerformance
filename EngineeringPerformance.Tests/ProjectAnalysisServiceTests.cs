using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Регрессии для <see cref="ProjectAnalysisService"/>:
/// 1) маппинг PEPM → перцентиль на диапазоне [-1, 1] (since 2026-05-05);
/// 2) AccuracyScore + список характеристик, формирующих PD%.
/// </summary>
public sealed class ProjectAnalysisServiceTests
{
    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContextFactory(options);
    }

    private static ProjectAnalysisService BuildService(IDbContextFactory<AppDbContext> factory)
    {
        var perfService = new PerformanceService(factory);
        var charService = new ProjectCharacteristicSelectionService(factory);
        return new ProjectAnalysisService(factory, perfService, charService);
    }

    /// <summary>
    /// Для одного корневого направления PEPM = Z-score этого направления.
    /// Сидим Z = targetZ (через factEstimate, normEstimate=0, std=1).
    /// </summary>
    private static async Task SeedSingleRootWithZAsync(IDbContextFactory<AppDbContext> factory, decimal targetZ)
    {
        await using var seed = factory.CreateDbContext();
        seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
        seed.IicWorkDirections.Add(new IicWorkDirection { Id = 10, Name = "root", Code = "R" });
        seed.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
        {
            Id = 100,
            WorkDirectionId = 10,
            Performance = 1m,
            Estimate = 0m,
            StandardDeviation = 1m,
            EffectiveFrom = DateTime.UtcNow.AddDays(-30),
            EffectiveTo = null
        });
        seed.PhysicalVolumes.Add(new PhysicalVolume { Id = 50, Code = "PV", Name = "pv", ClassifierId = 10, Coefficient = 1m, VolumeTypeId = 1, IsAssessment = true });
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

        // factProductivity = exp(targetZ) (т.к. normEstimate=0, std=1 → Z = factEstimate = ln(factProductivity)).
        // Берём volume = 1, hours = exp(targetZ).
        var hours = (decimal)Math.Exp((double)targetZ);
        seed.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume { Id = 200, ProjectId = 1, ProjectAnalysisVersionId = 1, PhysicalVolumeId = 50, Quantity = 1m });
        seed.ProjectLaborIntensities.Add(new ProjectLaborIntensity { Id = 300, ProjectId = 1, ProjectAnalysisVersionId = 1, WorkDirectionId = 10, ActualHours = hours, PlannedHours = hours, DocumentationCode = "DOC-1" });
        await seed.SaveChangesAsync();
    }

    [Theory]
    // Inside-range: 10 равных корзин (шаг 0.2)
    [InlineData(-0.95, "0-10-й")]
    [InlineData(-0.7, "10-20-й")]
    [InlineData(-0.5, "20-30-й")]
    [InlineData(-0.3, "30-40-й")]
    [InlineData(-0.1, "40-50-й")]
    [InlineData(0.0, "50-60-й")]
    [InlineData(0.1, "50-60-й")]
    [InlineData(0.3, "60-70-й")]
    [InlineData(0.5, "70-80-й")]
    [InlineData(0.7, "80-90-й")]
    [InlineData(0.9, "90-100-й")]
    // Out-of-range: клэмп
    [InlineData(-1.5, "0-й")]
    [InlineData(1.5, "100-й")]
    public async Task GenerateReport_PercentileMapping_FollowsLinearPepmRange(double targetZ, string expectedPercentile)
    {
        var factory = CreateFactory();
        await SeedSingleRootWithZAsync(factory, (decimal)targetZ);

        var service = BuildService(factory);
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.Percentile.Should().Be(expectedPercentile);
    }

    [Fact]
    public async Task GenerateReport_NoPepmData_PercentileIsUnavailable()
    {
        var factory = CreateFactory();
        await using (var seed = factory.CreateDbContext())
        {
            seed.Projects.Add(new Project { Id = 1, Code = "PRJ", Name = "test" });
            seed.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
            {
                Id = 1,
                ProjectId = 1,
                VersionName = "v1",
                CreatedAt = DateTime.UtcNow,
                Direction = AnalysisDirection.Fact
            });
            await seed.SaveChangesAsync();
        }

        var service = BuildService(factory);
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.PermIndicator.Should().BeNull();
        report.Percentile.Should().Be("Н/Д");
    }

    [Fact]
    public async Task GenerateReport_AccuracyCharacteristics_PopulatedFromProjectRows()
    {
        var factory = CreateFactory();
        await SeedSingleRootWithZAsync(factory, 0m);

        // Добавляем характеристики проекта: две с PD%, одна без значения.
        await using (var seed = factory.CreateDbContext())
        {
            seed.ProjectCharacteristics.Add(new ProjectCharacteristic { Id = 1, Name = "Стадия", SortOrder = 1 });
            seed.ProjectCharacteristics.Add(new ProjectCharacteristic { Id = 2, Name = "Сложность", SortOrder = 2 });
            seed.ProjectCharacteristicOptions.Add(new ProjectCharacteristicOption { Id = 10, CharacteristicId = 1, Name = "ПД", ValuePercent = 6m });
            seed.ProjectCharacteristicOptions.Add(new ProjectCharacteristicOption { Id = 11, CharacteristicId = 2, Name = "Средняя", ValuePercent = 10m });
            seed.ProjectCharacteristicSelections.Add(new ProjectCharacteristicSelection { Id = 100, ProjectId = 1, CharacteristicId = 1, OptionId = 10 });
            seed.ProjectCharacteristicSelections.Add(new ProjectCharacteristicSelection { Id = 101, ProjectId = 1, CharacteristicId = 2, OptionId = 11 });
            await seed.SaveChangesAsync();
        }

        var service = BuildService(factory);
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.AccuracyCharacteristics.Should().HaveCount(2);
        report.AccuracyCharacteristics.Select(c => c.CharacteristicName).Should().BeEquivalentTo(new[] { "Стадия", "Сложность" });
        report.AccuracyCharacteristics.Select(c => c.OptionName).Should().BeEquivalentTo(new[] { "ПД", "Средняя" });
        report.AccuracyCharacteristics.Select(c => c.ValuePercent).Should().BeEquivalentTo(new decimal?[] { 6m, 10m });

        // Среднее PD% = (6 + 10) / 2 = 8 → «Высокая» (по ComparisonAccuracy: <9).
        report.AccuracyScore.Should().Be(8m);
        report.AccuracyInterpretation.Should().Be("Высокая");
    }

    [Fact]
    public async Task GenerateReport_NoCharacteristics_AccuracyScoreIsNull()
    {
        var factory = CreateFactory();
        await SeedSingleRootWithZAsync(factory, 0m);

        var service = BuildService(factory);
        var report = await service.GenerateReportAsync(projectId: 1, versionId: 1);

        report.AccuracyCharacteristics.Should().BeEmpty();
        report.AccuracyScore.Should().BeNull();
        // ComparisonAccuracy.FromValuePercent(null) → "—" (NotApplicable).
        report.AccuracyInterpretation.Should().Be("—");
    }
}
