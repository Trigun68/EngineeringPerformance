using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class AnalysisVersionRecalculationOrchestratorTests
{
    [Fact]
    public async Task AfterNormChangedAsync_RecalculatesOnlyVersionsInEffectivePeriod()
    {
        await using var context = CreateContext();
        SeedProject(context, projectId: 1);

        // Период нормы: 2026-01-01 … 2026-06-30
        context.ProjectAnalysisVersions.AddRange(
            Version(1, 10, new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), archived: false),
            Version(1, 11, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), archived: false),
            Version(1, 12, new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc), archived: false),
            Version(1, 13, new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc), archived: true));
        await context.SaveChangesAsync();

        var recalculation = new Mock<ILaborIntensityRecalculationService>();
        recalculation
            .Setup(x => x.RecalculateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var orchestrator = new AnalysisVersionRecalculationOrchestrator(context, recalculation.Object);

        var count = await orchestrator.AfterNormChangedAsync(
            workDirectionId: 1,
            effectiveFrom: new DateTime(2026, 1, 1),
            effectiveTo: new DateTime(2026, 6, 30));

        count.Should().Be(1);
        recalculation.Verify(
            x => x.RecalculateAsync(1, 10, It.IsAny<CancellationToken>()),
            Times.Once);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 11, It.IsAny<CancellationToken>()),
            Times.Never);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 12, It.IsAny<CancellationToken>()),
            Times.Never);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 13, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AfterNormChangedAsync_SkipsVersionsWithoutMatchingWorkDirectionSnapshot()
    {
        await using var context = CreateContext();
        SeedProject(context, projectId: 1);

        context.ProjectAnalysisVersions.AddRange(
            Version(1, 30, new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc), archived: false),
            Version(1, 31, new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc), archived: false));
        context.AnalysisVersionSnapshots.AddRange(
            new AnalysisVersionSnapshot
            {
                Id = 1,
                ProjectAnalysisVersionId = 30,
                WorkDirectionId = 1,
                NormId = 100,
            },
            new AnalysisVersionSnapshot
            {
                Id = 2,
                ProjectAnalysisVersionId = 31,
                WorkDirectionId = 99,
                NormId = 200,
            });
        await context.SaveChangesAsync();

        var recalculation = new Mock<ILaborIntensityRecalculationService>();
        recalculation
            .Setup(x => x.RecalculateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var orchestrator = new AnalysisVersionRecalculationOrchestrator(context, recalculation.Object);

        var count = await orchestrator.AfterNormChangedAsync(
            workDirectionId: 1,
            effectiveFrom: new DateTime(2026, 1, 1),
            effectiveTo: new DateTime(2026, 6, 30));

        count.Should().Be(1);
        recalculation.Verify(
            x => x.RecalculateAsync(1, 30, It.IsAny<CancellationToken>()),
            Times.Once);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 31, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AfterCatalogAssessmentOrCoefficientChangedAsync_Kfo_RecalculatesLinkedActiveVersions()
    {
        await using var context = CreateContext();
        SeedProject(context, projectId: 1);
        SeedProject(context, projectId: 2);

        context.ProjectAnalysisVersions.AddRange(
            Version(1, 20, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), archived: false),
            Version(1, 21, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), archived: true),
            Version(2, 22, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), archived: false));
        context.ProjectPhysicalVolumes.AddRange(
            new ProjectPhysicalVolume
            {
                Id = 1,
                ProjectId = 1,
                ProjectAnalysisVersionId = 20,
                PhysicalVolumeKfoId = 100,
                Quantity = 1,
            },
            new ProjectPhysicalVolume
            {
                Id = 2,
                ProjectId = 1,
                ProjectAnalysisVersionId = 21,
                PhysicalVolumeKfoId = 100,
                Quantity = 1,
            },
            new ProjectPhysicalVolume
            {
                Id = 3,
                ProjectId = 2,
                ProjectAnalysisVersionId = 22,
                PhysicalVolumeKfoId = 999,
                Quantity = 1,
            });
        await context.SaveChangesAsync();

        var recalculation = new Mock<ILaborIntensityRecalculationService>();
        recalculation
            .Setup(x => x.RecalculateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var orchestrator = new AnalysisVersionRecalculationOrchestrator(context, recalculation.Object);

        var count = await orchestrator.AfterCatalogAssessmentOrCoefficientChangedAsync(
            "physical-volumes-kfo",
            catalogId: 100);

        count.Should().Be(1);
        recalculation.Verify(
            x => x.RecalculateAsync(1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 21, It.IsAny<CancellationToken>()),
            Times.Never);
        recalculation.Verify(
            x => x.RecalculateAsync(It.IsAny<int>(), 22, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecalculateVersionAsync_DelegatesToLaborIntensityService()
    {
        await using var context = CreateContext();
        var recalculation = new Mock<ILaborIntensityRecalculationService>();
        recalculation
            .Setup(x => x.RecalculateAsync(5, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var orchestrator = new AnalysisVersionRecalculationOrchestrator(context, recalculation.Object);
        var count = await orchestrator.RecalculateVersionAsync(5, 50);

        count.Should().Be(1);
        recalculation.Verify(
            x => x.RecalculateAsync(5, 50, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static void SeedProject(AppDbContext context, int projectId)
    {
        if (context.Projects.Any(p => p.Id == projectId))
            return;

        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = $"P{projectId}",
            Code = $"C{projectId}",
        });
    }

    private static ProjectAnalysisVersion Version(int projectId, int id, DateTime createdAt, bool archived) =>
        new()
        {
            Id = id,
            ProjectId = projectId,
            Name = $"V{id}",
            VersionName = $"V{id}",
            Direction = AnalysisDirection.Plan,
            CreatedAt = createdAt,
            IsArchived = archived,
        };
}
