using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class ProductivityTrendAnalyticsServiceTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void Quantile_MedianOfOddList()
    {
        var q = ProductivityTrendAnalyticsService.Quantile([1m, 3m, 10m], 0.5);
        q.Should().Be(3m);
    }

    [Fact]
    public async Task GetProductivityTrend_LinearToLog_StandardEffectMatchesEngine()
    {
        var seeded = await SeedAsync();
        await using var context = seeded.Context;
        var service = CreateService(seeded.Analytics);

        var result = await service.GetProductivityTrendAsync(new ProductivityTrendRequest(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            VolumeMode: "median"));

        result.ByGroup.Should().ContainSingle();
        result.ByGroup[0].VStar.Should().Be(10);
        result.Transitions.Should().ContainSingle();

        var linear = NormSnapshotFactory.FromEntity(LinearNorm());
        var log = NormSnapshotFactory.FromEntity(LogNorm());
        var expectedOld = _engine.ComputeForecastHours(
            linear.NormFormula, linear.DeviationFormula, 10m, 10m, PlanningZParameters.Default);
        var expectedNew = _engine.ComputeForecastHours(
            log.NormFormula, log.DeviationFormula, 10m, 10m, PlanningZParameters.Default);
        var expectedStandard = expectedNew.Base!.Value - expectedOld.Base!.Value;

        var transition = result.Transitions[0];
        transition.OldFormulaType.Should().Be(nameof(FormulaType.Linear));
        transition.NewFormulaType.Should().Be(nameof(FormulaType.Logarithmic));
        transition.StandardEffectHours.Should().BeApproximately((double)expectedStandard, 0.011);
        transition.MixEffectHours.Should().BeApproximately(0, 0.011);
        transition.Verdict.Should().Be("tightening");
        transition.CurvesCross.Should().BeFalse();

        result.Warnings.Should().Contain(w => w.Contains("NormId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetProductivityTrend_LogVolumeBelowOne_SetsFlag()
    {
        var seeded = await SeedAsync(volume: 0.5m);
        await using var context = seeded.Context;
        var service = CreateService(seeded.Analytics);

        var result = await service.GetProductivityTrendAsync(new ProductivityTrendRequest(
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            VolumeMode: "median"));

        result.Transitions.Should().ContainSingle();
        result.Transitions[0].LogVolumeLtOne.Should().BeTrue();
        result.ByGroup[0].Segments
            .SelectMany(s => s.Points)
            .Any(p => p.Flag == "log_volume_lt_one")
            .Should().BeTrue();
    }

    private ProductivityTrendAnalyticsService CreateService(IAnalyticsDbContextFactory analytics)
    {
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(x => x.UserId).Returns(1);
        var visibility = new Mock<IProjectVisibilityService>();
        visibility
            .Setup(x => x.GetAccessibleProjectIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 1 });

        return new ProductivityTrendAnalyticsService(
            analytics,
            user.Object,
            visibility.Object,
            _engine,
            new NullCache(),
            Options.Create(new AnalyticsTrendsOptions { CacheTtlMinutes = 1 }));
    }

    private static IicWorkDirectionNorm LinearNorm() => new()
    {
        Id = 101,
        WorkDirectionId = 7,
        FormulaType = FormulaType.Linear,
        CoefficientsJson = """{"a":10,"b":0.1}""",
        MinVolume = 0m,
        MaxVolume = 1000m,
        DeviationModelType = DeviationModelType.Multiplicative,
        DeviationCoefficientsJson = """{"sigma":0}""",
        EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static IicWorkDirectionNorm LogNorm() => new()
    {
        Id = 102,
        WorkDirectionId = 7,
        FormulaType = FormulaType.Logarithmic,
        CoefficientsJson = """{"a":10,"b":0.5}""",
        MinVolume = 0m,
        MaxVolume = 1000m,
        DeviationModelType = DeviationModelType.Multiplicative,
        DeviationCoefficientsJson = """{"sigma":0}""",
        EffectiveFrom = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static async Task<Seed> SeedAsync(decimal volume = 10m)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        context.Projects.Add(new Project { Id = 1, Name = "P1", Code = "C1" });
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 7, Name = "Направление", Code = "D7" });
        context.IicWorkDirectionNorms.AddRange(LinearNorm(), LogNorm());
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "Fact",
            VersionName = "Fact",
            Direction = AnalysisDirection.Fact,
            IsPrimary = true,
            CreatedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = 7,
            Coefficient = 1m,
        };
        context.PhysicalVolumesKfo.Add(kfo);
        context.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
        {
            Id = 1,
            ProjectId = 1,
            ProjectAnalysisVersionId = 10,
            Quantity = volume,
            PhysicalVolumeKfoId = 5,
            PhysicalVolumeKfo = kfo,
        });

        await context.SaveChangesAsync();
        return new Seed(context, new OptionsAnalyticsDbContextFactory(options));
    }

    private sealed record Seed(AppDbContext Context, IAnalyticsDbContextFactory Analytics);

    private sealed class OptionsAnalyticsDbContextFactory(DbContextOptions<AppDbContext> options)
        : IAnalyticsDbContextFactory
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class NullCache : IProjectAnalyticsCache
    {
        public Task<T?> GetAsync<T>(int projectId, string key, CancellationToken cancellationToken = default)
            where T : class =>
            Task.FromResult<T?>(null);

        public Task SetAsync<T>(int projectId, string key, T value, CancellationToken cancellationToken = default)
            where T : class =>
            Task.CompletedTask;

        public Task InvalidateProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> GetPortfolioAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class =>
            Task.FromResult<T?>(null);

        public Task SetPortfolioAsync<T>(string key, T value, CancellationToken cancellationToken = default)
            where T : class =>
            Task.CompletedTask;
    }
}
