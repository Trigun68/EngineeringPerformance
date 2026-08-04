using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.NormRegistry;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Registers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Registers;

public sealed class IicWorkDirectionNormServiceTests
{
    [Fact]
    public async Task CreateAsync_OverOpenPeriod_AutoClosesPreviousAndSucceeds()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection
        {
            Id = 7,
            Name = "Dir",
            Code = "D7",
        });
        context.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
        {
            Id = 1,
            WorkDirectionId = 7,
            Performance = 10m,
            EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheInvalidator>();
        cache.Setup(x => x.InvalidateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        recalc
            .Setup(x => x.AfterNormChangedAsync(
                7,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var registry = new Mock<INormRegistryService>();
        registry.Setup(x => x.InvalidateCacheAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = new IicWorkDirectionNormService(context, cache.Object, recalc.Object, registry.Object);

        var created = await service.CreateAsync(new IicNormWriteRequest(
            WorkDirectionId: 7,
            Performance: 12m,
            Estimate: 9m,
            StandardDeviation: 1.1m,
            UnitId: null,
            EffectiveFrom: new DateTime(2021, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: null));

        created.RecalculatedVersionCount.Should().Be(3);
        created.EffectiveFrom.Date.Should().Be(new DateTime(2021, 6, 15));

        var previous = await context.IicWorkDirectionNorms.AsNoTracking()
            .SingleAsync(n => n.Id == 1);
        previous.EffectiveTo.Should().Be(new DateTime(2021, 6, 14, 0, 0, 0, DateTimeKind.Utc));

        recalc.Verify(
            x => x.AfterNormChangedAsync(
                7,
                new DateTime(2021, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_TriggersRecalculationForPeriod()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection
        {
            Id = 5,
            Name = "Dir",
            Code = "D5",
        });
        context.IicWorkDirectionNorms.Add(new IicWorkDirectionNorm
        {
            Id = 10,
            WorkDirectionId = 5,
            Performance = 8m,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var cache = new Mock<ICacheInvalidator>();
        cache.Setup(x => x.InvalidateAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        recalc
            .Setup(x => x.AfterNormChangedAsync(
                5,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var registry = new Mock<INormRegistryService>();
        registry.Setup(x => x.InvalidateCacheAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = new IicWorkDirectionNormService(context, cache.Object, recalc.Object, registry.Object);

        var updated = await service.UpdateAsync(10, new IicNormWriteRequest(
            WorkDirectionId: 5,
            Performance: 9m,
            Estimate: null,
            StandardDeviation: null,
            UnitId: null,
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)));

        updated.Should().NotBeNull();
        updated!.RecalculatedVersionCount.Should().Be(2);
        updated.Performance.Should().Be(9m);

        recalc.Verify(
            x => x.AfterNormChangedAsync(
                5,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
