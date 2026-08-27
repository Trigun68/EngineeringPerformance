using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using EngineeringPerformance.Infrastructure.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Projects;

public sealed class ProjectDataServiceRecalcTests
{
    [Fact]
    public async Task DeletePhysicalVolume_RecalculatesVersion()
    {
        await using var context = CreateContext();
        context.Projects.Add(new Project { Id = 1, Name = "P", Code = "C" });
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "V",
            VersionName = "V",
        });
        context.ProjectPhysicalVolumes.Add(new ProjectPhysicalVolume
        {
            Id = 100,
            ProjectId = 1,
            ProjectAnalysisVersionId = 10,
            Quantity = 1m,
        });
        await context.SaveChangesAsync();

        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        recalc.Setup(x => x.RecalculateVersionAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var cache = new Mock<IProjectAnalyticsCache>();
        var service = new ProjectDataService(context, cache.Object, recalc.Object);

        (await service.DeletePhysicalVolumeAsync(1, 10, 100)).Should().BeTrue();

        recalc.Verify(x => x.RecalculateVersionAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.InvalidateProjectAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEquipment_RecalculatesVersion()
    {
        await using var context = CreateContext();
        context.Projects.Add(new Project { Id = 1, Name = "P", Code = "C" });
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "V",
            VersionName = "V",
        });
        context.Equipments.Add(new Equipment { Id = 8, Name = "E", CatalogKeyHash = "h" });
        context.ProjectEquipments.Add(new ProjectEquipment
        {
            Id = 200,
            ProjectId = 1,
            ProjectAnalysisVersionId = 10,
            EquipmentId = 8,
            Quantity = 1m,
        });
        await context.SaveChangesAsync();

        var recalc = new Mock<IAnalysisVersionRecalculationOrchestrator>();
        recalc.Setup(x => x.RecalculateVersionAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new ProjectDataService(
            context,
            new Mock<IProjectAnalyticsCache>().Object,
            recalc.Object);

        (await service.DeleteEquipmentAsync(1, 10, 200)).Should().BeTrue();
        recalc.Verify(x => x.RecalculateVersionAsync(1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
