using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// ISS-018: кэш тяжёлой аналитики отдаёт данные повторно, но любая мутация проекта
/// (или норм ИИС) обязана сделать записи недостижимыми — иначе UI покажет старые цифры.
/// </summary>
public sealed class ProjectAnalyticsCacheTests
{
    private sealed record Payload(string Value);

    private static ProjectAnalyticsCache CreateCache()
    {
        IDistributedCache distributed = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        return new ProjectAnalyticsCache(distributed, NullLogger<ProjectAnalyticsCache>.Instance);
    }

    [Fact]
    public async Task Get_ReturnsStoredValueForSameProjectAndKey()
    {
        var cache = CreateCache();
        await cache.SetAsync(1, "page-data:v10", new Payload("grids"));

        var hit = await cache.GetAsync<Payload>(1, "page-data:v10");

        hit.Should().NotBeNull();
        hit!.Value.Should().Be("grids");
    }

    [Fact]
    public async Task Get_ReturnsNullForDifferentProjectOrKey()
    {
        var cache = CreateCache();
        await cache.SetAsync(1, "page-data:v10", new Payload("grids"));

        (await cache.GetAsync<Payload>(2, "page-data:v10")).Should().BeNull("кэш разделён по проектам");
        (await cache.GetAsync<Payload>(1, "page-data:v11")).Should().BeNull("другая версия — другой ключ");
    }

    [Fact]
    public async Task InvalidateProject_DropsOnlyThatProject()
    {
        var cache = CreateCache();
        await cache.SetAsync(1, "report:auto", new Payload("first"));
        await cache.SetAsync(2, "report:auto", new Payload("second"));

        await cache.InvalidateProjectAsync(1);

        (await cache.GetAsync<Payload>(1, "report:auto")).Should().BeNull();
        (await cache.GetAsync<Payload>(2, "report:auto"))!.Value.Should().Be("second");
    }

    [Fact]
    public async Task InvalidateAll_DropsEveryProject()
    {
        var cache = CreateCache();
        await cache.SetAsync(1, "report:auto", new Payload("first"));
        await cache.SetAsync(2, "report:auto", new Payload("second"));

        await cache.InvalidateAllAsync();

        (await cache.GetAsync<Payload>(1, "report:auto")).Should().BeNull();
        (await cache.GetAsync<Payload>(2, "report:auto")).Should().BeNull();
    }

    [Fact]
    public async Task SetAfterInvalidate_IsVisibleAgain()
    {
        var cache = CreateCache();
        await cache.SetAsync(1, "report:auto", new Payload("stale"));
        await cache.InvalidateProjectAsync(1);

        await cache.SetAsync(1, "report:auto", new Payload("fresh"));

        (await cache.GetAsync<Payload>(1, "report:auto"))!.Value.Should().Be("fresh");
    }
}
