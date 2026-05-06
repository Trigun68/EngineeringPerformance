using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace EngineeringPerformance.Tests;

public sealed class DirectoryCacheTests
{
    public sealed class DummyItem : DirectoryItem { }

    private static IReadOnlyList<DummyItem> Sample(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new DummyItem { Id = i, Name = $"item-{i}" })
            .ToList();

    [Fact]
    public async Task GetAllAsync_FirstCall_LoadsFromService()
    {
        var data = Sample(3);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);

        var result = await cache.GetAllAsync();

        result.Should().BeEquivalentTo(data);
        svc.Verify(s => s.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SecondCall_HitsCacheAndDoesNotReloadService()
    {
        var data = Sample(3);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);

        await cache.GetAllAsync();
        await cache.GetAllAsync();

        svc.Verify(s => s.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task Invalidate_NextCall_ReloadsFromService()
    {
        var data = Sample(2);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);

        await cache.GetAllAsync();
        cache.Invalidate();
        await cache.GetAllAsync();

        svc.Verify(s => s.GetAllAsync(false), Times.Exactly(2));
    }

    [Fact]
    public async Task Upsert_ReplacesExistingItem()
    {
        var data = Sample(3);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);
        await cache.GetAllAsync();

        cache.Upsert(new DummyItem { Id = 2, Name = "renamed" });

        var result = await cache.GetAllAsync();
        result.Should().Contain(x => x.Id == 2 && x.Name == "renamed");
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Upsert_AddsNewItemWhenIdNotPresent()
    {
        var data = Sample(2);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);
        await cache.GetAllAsync();

        cache.Upsert(new DummyItem { Id = 99, Name = "new" });

        var result = await cache.GetAllAsync();
        result.Should().HaveCount(3);
        result.Should().Contain(x => x.Id == 99);
    }

    [Fact]
    public void Upsert_BeforeWarmUp_NoOps()
    {
        var svc = new Mock<IDirectoryService<DummyItem>>();
        var cache = new DirectoryCache<DummyItem>(svc.Object);

        // Не должен ни падать, ни пытаться загружать данные — кеш холодный.
        cache.Upsert(new DummyItem { Id = 1, Name = "x" });

        svc.Verify(s => s.GetAllAsync(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Remove_RemovesItemFromCache()
    {
        var data = Sample(3);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        svc.Setup(s => s.GetAllAsync(false)).ReturnsAsync(data);

        var cache = new DirectoryCache<DummyItem>(svc.Object);
        await cache.GetAllAsync();

        cache.Remove(2);

        var result = await cache.GetAllAsync();
        result.Should().HaveCount(2);
        result.Should().NotContain(x => x.Id == 2);
    }

    [Fact]
    public async Task ConcurrentGetAllAsync_LoadsServiceOnlyOnce()
    {
        var data = Sample(5);
        var svc = new Mock<IDirectoryService<DummyItem>>();
        var loadGate = new TaskCompletionSource<IReadOnlyList<DummyItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.Setup(s => s.GetAllAsync(false)).Returns(loadGate.Task);

        var cache = new DirectoryCache<DummyItem>(svc.Object);

        // 10 параллельных вызовов до того, как сервис вернёт данные.
        var tasks = Enumerable.Range(0, 10).Select(_ => cache.GetAllAsync()).ToArray();
        loadGate.SetResult(data);

        await Task.WhenAll(tasks);

        svc.Verify(s => s.GetAllAsync(false), Times.Once);
        tasks.Select(t => t.Result).Should().AllSatisfy(r => r.Should().BeEquivalentTo(data));
    }
}
