using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class ForecastPlannedVolumePersistenceServiceTests
{
    [Fact]
    public async Task Save_ByProject_DoesNotZeroSiblings()
    {
        var seeded = await SeedAsync(PlanningScenario.ByProjectIicGroup);
        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        var service = new ForecastPlannedVolumePersistenceService(
            seeded.Factory, recalc.Object, Mock.Of<IUserActionLogger>());

        await service.SavePlannedVolumeAsync(1, 10, 7, 20m);

        var rows = await seeded.Context.ProjectPhysicalVolumes.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(r => r.Quantity > 0m);
        (rows[0].Quantity + rows[1].Quantity).Should().Be(20m);
        recalc.Verify(r => r.RecalculateVersionAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_ByEachValue_MultipleRows_ThrowsAndKeepsQuantities()
    {
        var seeded = await SeedAsync(PlanningScenario.ByEachValue);
        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        var service = new ForecastPlannedVolumePersistenceService(
            seeded.Factory, recalc.Object, Mock.Of<IUserActionLogger>());

        var act = () => service.SavePlannedVolumeAsync(1, 10, 7, 20m);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(PlannedVolumeDistributor.EachValueMultipleRowsMessage);

        var rows = await seeded.Context.ProjectPhysicalVolumes.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        rows[0].Quantity.Should().Be(2m);
        rows[1].Quantity.Should().Be(8m);
        recalc.Verify(r => r.RecalculateVersionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task<Seed> SeedAsync(PlanningScenario scenario)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Projects.Add(new Project { Id = 1, Name = "P1", Code = "C1" });
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 7, Name = "Dir", Code = "D7" });
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "V",
            VersionName = "V",
            Direction = AnalysisDirection.Plan,
            PlanningScenario = scenario,
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
        context.ProjectPhysicalVolumes.AddRange(
            new ProjectPhysicalVolume
            {
                Id = 1,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                Quantity = 2m,
                FacilityCode = "OBJ-A",
                PhysicalVolumeKfoId = 5,
                PhysicalVolumeKfo = kfo,
            },
            new ProjectPhysicalVolume
            {
                Id = 2,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                Quantity = 8m,
                FacilityCode = "OBJ-B",
                PhysicalVolumeKfoId = 5,
                PhysicalVolumeKfo = kfo,
            });
        await context.SaveChangesAsync();
        return new Seed(context, new Factory(options));
    }

    private sealed record Seed(AppDbContext Context, IDbContextFactory<AppDbContext> Factory);

    private sealed class Factory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }
}
