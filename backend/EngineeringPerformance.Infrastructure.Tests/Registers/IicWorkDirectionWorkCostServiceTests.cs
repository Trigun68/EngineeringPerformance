using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Registers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Registers;

public sealed class IicWorkDirectionWorkCostServiceTests
{
    [Fact]
    public async Task SaveBatch_Organization_WritesOneRowPerDirectionAndAutoCloses()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.AddRange(
            new IicWorkDirection { Id = 1, Name = "Root", Code = "R1" },
            new IicWorkDirection { Id = 2, Name = "Child", Code = "C2", ParentId = 1 });
        context.IicWorkDirectionWorkCosts.Add(new IicWorkDirectionWorkCost
        {
            Id = 10,
            WorkDirectionId = 1,
            RatePerHour = 100m,
            EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        var result = await service.SaveBatchAsync(new WorkCostBatchRequest(
            new DateTime(2021, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            WorkCostLevel.Organization,
            [new WorkCostBatchItem(0, 250m)]));

        result.WrittenCount.Should().Be(2);

        var previous = await context.IicWorkDirectionWorkCosts.AsNoTracking()
            .SingleAsync(c => c.Id == 10);
        previous.EffectiveTo.Should().Be(new DateTime(2021, 6, 14, 0, 0, 0, DateTimeKind.Utc));

        var asOf = await service.GetAsOfAsync(new DateTime(2021, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        asOf.Should().HaveCount(2);
        asOf.Should().OnlyContain(r => r.RatePerHour == 250m);
        asOf.Should().OnlyContain(r => r.WorkDirectionName.Contains('—') || r.WorkDirectionName.Length > 0);
    }

    [Fact]
    public async Task SaveBatch_SameEffectiveFrom_UpdatesRate()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 7, Name = "Dir", Code = "D7" });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await service.SaveBatchAsync(new WorkCostBatchRequest(
            from,
            WorkCostLevel.EachElement,
            [new WorkCostBatchItem(7, 10m)]));
        await service.SaveBatchAsync(new WorkCostBatchRequest(
            from,
            WorkCostLevel.EachElement,
            [new WorkCostBatchItem(7, 15m)]));

        var rows = await context.IicWorkDirectionWorkCosts.AsNoTracking().ToListAsync();
        rows.Should().ContainSingle();
        rows[0].RatePerHour.Should().Be(15m);
    }

    [Fact]
    public async Task GetAsOf_ReturnsDirectionsWithoutRate()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 3, Name = "Empty", Code = "E3" });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        var rows = await service.GetAsOfAsync(DateTime.UtcNow);

        rows.Should().ContainSingle();
        rows[0].RatePerHour.Should().BeNull();
        rows[0].PeriodId.Should().BeNull();
        rows[0].WorkDirectionId.Should().Be(3);
    }

    [Fact]
    public async Task ListPeriods_GroupsByEffectiveFrom()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.AddRange(
            new IicWorkDirection { Id = 1, Name = "A", Code = "A" },
            new IicWorkDirection { Id = 2, Name = "B", Code = "B" });
        context.IicWorkDirectionWorkCosts.AddRange(
            new IicWorkDirectionWorkCost
            {
                WorkDirectionId = 1,
                RatePerHour = 10m,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            },
            new IicWorkDirectionWorkCost
            {
                WorkDirectionId = 2,
                RatePerHour = 10m,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            },
            new IicWorkDirectionWorkCost
            {
                WorkDirectionId = 1,
                RatePerHour = 20m,
                EffectiveFrom = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var periods = await new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>()).ListPeriodsAsync();

        periods.Should().HaveCount(2);
        periods[0].EffectiveFrom.Date.Should().Be(new DateTime(2025, 6, 1));
        periods[0].DirectionCount.Should().Be(1);
        periods[0].UniformRate.Should().Be(20m);
        periods[1].DirectionCount.Should().Be(2);
        periods[1].UniformRate.Should().Be(10m);
    }

    [Fact]
    public async Task GetByEffectiveFrom_DoesNotInheritOlderPeriod()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.AddRange(
            new IicWorkDirection { Id = 1, Name = "A", Code = "A" },
            new IicWorkDirection { Id = 2, Name = "B", Code = "B" });
        context.IicWorkDirectionWorkCosts.AddRange(
            new IicWorkDirectionWorkCost
            {
                WorkDirectionId = 1,
                RatePerHour = 10m,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            },
            new IicWorkDirectionWorkCost
            {
                WorkDirectionId = 1,
                RatePerHour = 30m,
                EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        var block = await service.GetByEffectiveFromAsync(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        block.Should().HaveCount(2);
        block.Single(r => r.WorkDirectionId == 1).RatePerHour.Should().Be(30m);
        block.Single(r => r.WorkDirectionId == 2).RatePerHour.Should().BeNull();
    }

    [Fact]
    public async Task SaveBatch_InsertsBetweenExistingPeriods_ClosesNewAtDayBeforeNext()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 1, Name = "A", Code = "A" });
        context.IicWorkDirectionWorkCosts.AddRange(
            new IicWorkDirectionWorkCost
            {
                Id = 1,
                WorkDirectionId = 1,
                RatePerHour = 10m,
                EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            },
            new IicWorkDirectionWorkCost
            {
                Id = 2,
                WorkDirectionId = 1,
                RatePerHour = 90m,
                EffectiveFrom = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = null,
                CreatedAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        await service.SaveBatchAsync(new WorkCostBatchRequest(
            new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            WorkCostLevel.EachElement,
            [new WorkCostBatchItem(1, 40m)]));

        var rows = await context.IicWorkDirectionWorkCosts.AsNoTracking()
            .OrderBy(c => c.EffectiveFrom)
            .ToListAsync();
        rows.Should().HaveCount(3);
        rows[0].Id.Should().Be(1);
        rows[0].EffectiveTo.Should().Be(new DateTime(2024, 6, 14, 0, 0, 0, DateTimeKind.Utc));
        rows[1].RatePerHour.Should().Be(40m);
        rows[1].EffectiveFrom.Should().Be(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        rows[1].EffectiveTo.Should().Be(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        rows[2].Id.Should().Be(2);
        rows[2].EffectiveFrom.Should().Be(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        rows[2].EffectiveTo.Should().BeNull();
    }

    [Fact]
    public async Task DeletePeriod_RemovesBlockAndReopensPrevious()
    {
        await using var context = CreateContext();
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 1, Name = "A", Code = "A" });
        context.IicWorkDirectionWorkCosts.AddRange(
            new IicWorkDirectionWorkCost
            {
                Id = 1,
                WorkDirectionId = 1,
                RatePerHour = 10m,
                EffectiveFrom = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow,
            },
            new IicWorkDirectionWorkCost
            {
                Id = 2,
                WorkDirectionId = 1,
                RatePerHour = 40m,
                EffectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EffectiveTo = null,
                CreatedAt = DateTime.UtcNow,
            });
        await context.SaveChangesAsync();

        var service = new IicWorkDirectionWorkCostService(context, Mock.Of<IUserActionLogger>());
        var result = await service.DeletePeriodAsync(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.DeletedCount.Should().Be(1);
        var remaining = await context.IicWorkDirectionWorkCosts.AsNoTracking().ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].Id.Should().Be(1);
        remaining[0].EffectiveTo.Should().BeNull();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
