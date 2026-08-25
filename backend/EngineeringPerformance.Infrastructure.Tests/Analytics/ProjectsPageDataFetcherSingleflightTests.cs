using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class PageDataBuildCoalescerTests
{
    [Fact]
    public async Task RunAsync_SharesOneBuildForParallelWaiters()
    {
        var coalescer = new PageDataBuildCoalescer();
        var builds = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<ProjectsPageDataEncodedDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Task<ProjectsPageDataEncodedDto> Build()
        {
            Interlocked.Increment(ref builds);
            started.TrySetResult();
            return finish.Task;
        }

        var first = coalescer.RunAsync("k", Build, CancellationToken.None);
        await started.Task;
        var second = coalescer.RunAsync("k", Build, CancellationToken.None);
        finish.TrySetResult(DummyEncoded());

        var results = await Task.WhenAll(first, second);
        builds.Should().Be(1);
        results[0].Should().BeSameAs(results[1]);
    }

    private static ProjectsPageDataEncodedDto DummyEncoded() =>
        ProjectsPageDataEncodedDto.From(new ProjectsPageDataDto(
            new ProjectsPageForecastGridsDto([], [], [], new ForecastScenarioTotalsDto(0, 0, 0)),
            new ProjectsPageFactGridsDto([], [], []),
            null));
}

public sealed class ProjectsPageDataFetcherSingleflightTests
{
    [Fact]
    public async Task SummaryAndDetail_ShareOneLoadAsync()
    {
        var loads = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = new Mock<IProjectsPageRawDataLoader>();
        loader
            .Setup(x => x.LoadAsync(1, 10, It.IsAny<CancellationToken>()))
            .Returns(async (int _, int _, CancellationToken ct) =>
            {
                Interlocked.Increment(ref loads);
                await release.Task.WaitAsync(ct);
                return EmptyBundle();
            });

        var fetcher = CreateFetcher(loader.Object);
        var summary = fetcher.GetEncodedPageDataAsync(1, 10);
        var detail = fetcher.GetEncodedPageDetailsAsync(1, 10);

        await Task.Delay(50);
        loads.Should().Be(1);
        release.TrySetResult();

        await Task.WhenAll(summary, detail);
        loads.Should().Be(1);
        loader.Verify(x => x.LoadAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ProjectsPageDataFetcher CreateFetcher(IProjectsPageRawDataLoader loader)
    {
        IDistributedCache distributed = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        var cache = new ProjectAnalyticsCache(distributed, NullLogger<ProjectAnalyticsCache>.Instance);
        var engine = new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));
        return new ProjectsPageDataFetcher(loader, engine, cache, new PageDataBuildCoalescer());
    }

    private static ProjectsPageVersionBundle EmptyBundle() => new()
    {
        VersionExists = true,
        WorkDirections = [],
        PhysicalVolumes = [],
        Equipment = [],
        LaborIntensities = [],
        NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
        ClassifierMap = new Dictionary<int, IicWorkDirection>(),
        PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
        NormIdByDirection = new Dictionary<int, int>(),
    };
}
