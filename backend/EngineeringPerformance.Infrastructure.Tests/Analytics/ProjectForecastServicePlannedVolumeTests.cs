using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class ProjectForecastServicePlannedVolumeTests
{
    [Fact]
    public void GroupPlannedPhysicalVolumes_EmptyClaimed_PutsSubtreeTotalOnRootsOnly()
    {
        var parent = new Dictionary<int, int?> { [1] = null, [11] = 1, [12] = 1 };
        var direct = new Dictionary<int, decimal> { [1] = 2m, [11] = 10m, [12] = 6m };
        var total = new Dictionary<int, decimal> { [1] = 18m, [11] = 10m, [12] = 6m };

        var grouped = ProjectForecastService.GroupPlannedPhysicalVolumes(
            direct, total, new HashSet<int>(), parent);

        grouped.Should().ContainKey(1).WhoseValue.Should().Be(18m);
        grouped.Should().NotContainKey(11);
        grouped.Should().NotContainKey(12);
    }

    [Fact]
    public void GroupPlannedPhysicalVolumes_Exclusive_ReusesIicClaimedBuckets()
    {
        var parent = new Dictionary<int, int?> { [1] = null, [11] = 1, [12] = 1 };
        var direct = new Dictionary<int, decimal> { [1] = 2m, [11] = 10m, [12] = 6m };
        var total = new Dictionary<int, decimal> { [1] = 18m, [11] = 10m, [12] = 6m };
        var claimed = new HashSet<int> { 1, 11 };

        var grouped = ProjectForecastService.GroupPlannedPhysicalVolumes(
            direct, total, claimed, parent);
        var exclusive = IicClaimedBuckets.ApplyExclusiveVolumes(direct, claimed, parent);

        grouped.Should().BeEquivalentTo(exclusive);
        grouped[1].Should().Be(8m);
        grouped[11].Should().Be(10m);
        grouped.Should().NotContainKey(12);
    }

    [Fact]
    public async Task GenerateForecast_OwnVolumeIsDirect_GroupedFollowsEmptyClaimedRoots()
    {
        var factory = await SeedAsync(claimChild11: false);
        var service = new ProjectForecastService(factory);

        var report = await service.GenerateForecastAsync(1, 10);

        var root = report.PlannedVolumes.Should().ContainSingle(r => r.WorkDirectionId == 1).Subject;
        var child = report.PlannedVolumes.Should().ContainSingle(r => r.WorkDirectionId == 11).Subject;

        root.PlannedVolume.Should().Be(18m);
        root.OwnPlannedVolume.Should().Be(2m);
        root.GroupedPhysicalVolume.Should().Be(18m);

        child.PlannedVolume.Should().Be(10m);
        child.OwnPlannedVolume.Should().Be(10m);
        child.GroupedPhysicalVolume.Should().BeNull();
    }

    [Fact]
    public async Task GenerateForecast_GroupedVolumeFollowsClaimedAsOfBindings()
    {
        var factory = await SeedAsync(claimChild11: true);
        var service = new ProjectForecastService(factory);

        var report = await service.GenerateForecastAsync(1, 10);

        var root = report.PlannedVolumes.Should().ContainSingle(r => r.WorkDirectionId == 1).Subject;
        var claimed = report.PlannedVolumes.Should().ContainSingle(r => r.WorkDirectionId == 11).Subject;
        var sibling = report.PlannedVolumes.Should().ContainSingle(r => r.WorkDirectionId == 12).Subject;

        root.PlannedVolume.Should().Be(18m);
        root.OwnPlannedVolume.Should().Be(2m);
        root.GroupedPhysicalVolume.Should().Be(8m);

        claimed.OwnPlannedVolume.Should().Be(10m);
        claimed.GroupedPhysicalVolume.Should().Be(10m);

        sibling.OwnPlannedVolume.Should().Be(6m);
        sibling.GroupedPhysicalVolume.Should().BeNull();
    }

    private static async Task<IDbContextFactory<AppDbContext>> SeedAsync(bool claimChild11)
    {
        var asOf = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        context.Projects.Add(new Project { Id = 1, Name = "P1", Code = "C1" });
        context.IicWorkDirections.AddRange(
            new IicWorkDirection { Id = 1, Code = "R", Name = "Root", SortOrder = 1 },
            new IicWorkDirection { Id = 11, ParentId = 1, Code = "A", Name = "ChildA", SortOrder = 1 },
            new IicWorkDirection { Id = 12, ParentId = 1, Code = "B", Name = "ChildB", SortOrder = 2 });
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "V",
            VersionName = "V",
            Direction = AnalysisDirection.Plan,
            CreatedAt = asOf,
        });
        context.ProjectPhysicalVolumes.AddRange(
            Ppv(1, 1, 2m),
            Ppv(2, 11, 10m),
            Ppv(3, 12, 6m));

        if (claimChild11)
        {
            var from = DateTime.SpecifyKind(asOf.Date.AddDays(-10), DateTimeKind.Utc);
            context.LaborIntensities.AddRange(
                new LaborIntensity { Id = 50, Code = "LI-A", Name = "LI-A" },
                new LaborIntensity { Id = 51, Code = "LI-R", Name = "LI-R" });
            context.LaborIntensityIicBindings.AddRange(
                new LaborIntensityIicBinding
                {
                    Id = 1,
                    LaborIntensityId = 50,
                    IicWorkDirectionId = 11,
                    EffectiveFrom = from,
                },
                new LaborIntensityIicBinding
                {
                    Id = 2,
                    LaborIntensityId = 51,
                    IicWorkDirectionId = 1,
                    EffectiveFrom = from,
                });
        }

        await context.SaveChangesAsync();
        return new Factory(options);
    }

    private static ProjectPhysicalVolume Ppv(int id, int classifierId, decimal quantity) =>
        new()
        {
            Id = id,
            ProjectId = 1,
            ProjectAnalysisVersionId = 10,
            Quantity = quantity,
            CatalogCoefficient = 1m,
            CatalogIsAssessment = true,
            CatalogClassifierId = classifierId,
        };

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
