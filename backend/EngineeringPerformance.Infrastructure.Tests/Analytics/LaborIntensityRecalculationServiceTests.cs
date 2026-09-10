using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using EngineeringPerformance.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Persist пишет LaborIntensity* на ФО и оборудование тем же движком, что GET потом читает.
/// </summary>
public sealed class LaborIntensityRecalculationServiceTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Theory]
    [InlineData(PlanningScenario.ByEachValue)]
    [InlineData(PlanningScenario.ByProjectIicGroup)]
    [InlineData(PlanningScenario.ByObjectIicGroup)]
    public async Task RecalculateAsync_WritesPpvAndEquipmentHours_MatchingEngine(PlanningScenario scenario)
    {
        var seeded = await SeedAsync(scenario);
        await using var context = seeded.Context;
        var service = new LaborIntensityRecalculationService(seeded.Analytics, _engine);

        var updated = await service.RecalculateAsync(1, 10);

        updated.Should().BeGreaterThan(0);
        context.ChangeTracker.Clear();

        var ppv = await context.ProjectPhysicalVolumes.AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();
        var equipment = await context.ProjectEquipments.AsNoTracking()
            .Include(e => e.Equipment)
            .ToListAsync();

        var expected = _engine.RecalculatePlanHours(
            scenario,
            [
                new LaborIntensityCalculator.PpvLine(1, 7, "OBJ-A", 2m, 1m, true),
                new LaborIntensityCalculator.PpvLine(2, 7, "OBJ-B", 8m, 1m, true),
                new LaborIntensityCalculator.PpvLine(3, 7, "OBJ-A", 4m, 1m, false),
            ],
            [
                new LaborIntensityCalculator.EquipmentLine(20, 7, 5m, true, "OBJ-A"),
                new LaborIntensityCalculator.EquipmentLine(21, 7, 4m, false),
            ],
            WrightNorms(),
            new Dictionary<int, PlanningZParameters>());

        foreach (var row in expected.Ppv)
        {
            var stored = ppv.Single(p => p.Id == row.PpvId);
            stored.LaborIntensityBase.Should().Be(row.Base);
            stored.LaborIntensityPessimistic.Should().Be(row.Pessimistic);
            stored.LaborIntensityOptimistic.Should().Be(row.Optimistic);
        }

        ppv.Single(p => p.Id == 3).LaborIntensityBase.Should().BeNull("оценка «Нет» не получает LI");

        equipment.Single(e => e.Id == 20).LaborIntensityBase.Should().Be(expected.Equipment[20].Base);
        equipment.Single(e => e.Id == 21).LaborIntensityBase.Should().BeNull();

        var dir = await context.IicWorkDirections.AsNoTracking().SingleAsync();
        var kfoMap = await context.PhysicalVolumesKfo.AsNoTracking().ToDictionaryAsync(k => k.Id);
        foreach (var row in ppv)
        {
            if (row.PhysicalVolumeKfoId is int kfoId)
                row.PhysicalVolumeKfo = kfoMap[kfoId];
        }

        var eqCatalog = await context.Equipments.AsNoTracking().ToDictionaryAsync(e => e.Id);
        foreach (var row in equipment)
            row.Equipment = eqCatalog[row.EquipmentId];

        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [dir],
            PhysicalVolumes = ppv,
            Equipment = equipment,
            LaborIntensities = [],
            NormsByDirection = WrightNorms(),
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [7] = dir },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            PlanningScenario = scenario,
            NormIdByDirection = new Dictionary<int, int>(),
        };
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, []);
        var expectedTotal =
            expected.Ppv.Sum(r => r.Base ?? 0m) + (expected.Equipment[20].Base ?? 0m);
        forecast.GrandTotals.BaseHours.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task RecalculateAsync_EquipmentHours_FollowPpvScenario_WhenNormDependsOnVolume()
    {
        var each = await RecalculateEquipmentBaseAsync(PlanningScenario.ByEachValue);
        var project = await RecalculateEquipmentBaseAsync(PlanningScenario.ByProjectIicGroup);
        var obj = await RecalculateEquipmentBaseAsync(PlanningScenario.ByObjectIicGroup);

        each.Should().BePositive();
        project.Should().BePositive();
        obj.Should().BePositive();
        each.Should().NotBe(project);
        obj.Should().NotBe(each);
    }

    private async Task<decimal?> RecalculateEquipmentBaseAsync(PlanningScenario scenario)
    {
        var seeded = await SeedAsync(scenario);
        await using var context = seeded.Context;
        var service = new LaborIntensityRecalculationService(seeded.Analytics, _engine);
        await service.RecalculateAsync(1, 10);
        context.ChangeTracker.Clear();
        var eq = await context.ProjectEquipments.AsNoTracking().SingleAsync(e => e.Id == 20);
        return eq.LaborIntensityBase;
    }

    private static IReadOnlyDictionary<int, NormCalculationSnapshot> WrightNorms()
    {
        var snap = NormSnapshotFactory.FromEntity(WrightNorm());
        return new Dictionary<int, NormCalculationSnapshot> { [7] = snap };
    }

    private static IicWorkDirectionNorm WrightNorm() => new()
    {
        Id = 100,
        WorkDirectionId = 7,
        Performance = 100m,
        FormulaType = FormulaType.Wright,
        CoefficientsJson = """{"a":100,"b":0.2}""",
        MinVolume = 1m,
        MaxVolume = 1000m,
        DeviationModelType = DeviationModelType.Multiplicative,
        DeviationCoefficientsJson = """{"sigma":0}""",
        EffectiveFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    private static async Task<Seed> SeedAsync(PlanningScenario scenario)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        context.Projects.Add(new Project { Id = 1, Name = "P1", Code = "C1" });
        context.IicWorkDirections.Add(new IicWorkDirection { Id = 7, Name = "Dir", Code = "D7" });
        var norm = WrightNorm();
        context.IicWorkDirectionNorms.Add(norm);
        context.ProjectAnalysisVersions.Add(new ProjectAnalysisVersion
        {
            Id = 10,
            ProjectId = 1,
            Name = "V",
            VersionName = "V",
            PlanningScenario = scenario,
        });
        context.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
        {
            Id = 1,
            ProjectAnalysisVersionId = 10,
            WorkDirectionId = 7,
            NormId = 100,
            Norm = norm,
        });

        var kfoYes = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = 7,
            Coefficient = 1m,
        };
        var kfoNo = new PhysicalVolumeKfo
        {
            Id = 6,
            Code = "N",
            Name = "N",
            IsAssessment = false,
            ClassifierId = 7,
            Coefficient = 1m,
        };
        context.PhysicalVolumesKfo.AddRange(kfoYes, kfoNo);

        context.ProjectPhysicalVolumes.AddRange(
            new ProjectPhysicalVolume
            {
                Id = 1,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                Quantity = 2m,
                FacilityCode = "OBJ-A",
                PhysicalVolumeKfoId = 5,
                PhysicalVolumeKfo = kfoYes,
            },
            new ProjectPhysicalVolume
            {
                Id = 2,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                Quantity = 8m,
                FacilityCode = "OBJ-B",
                PhysicalVolumeKfoId = 5,
                PhysicalVolumeKfo = kfoYes,
            },
            new ProjectPhysicalVolume
            {
                Id = 3,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                Quantity = 4m,
                FacilityCode = "OBJ-A",
                PhysicalVolumeKfoId = 6,
                PhysicalVolumeKfo = kfoNo,
            });

        var eqYes = new Equipment
        {
            Id = 8,
            Name = "Pump",
            ClassifierId = 7,
            IsAssessment = true,
            Coefficient = 1m,
            CatalogKeyHash = "h1",
        };
        var eqNo = new Equipment
        {
            Id = 9,
            Name = "Skip",
            ClassifierId = 7,
            IsAssessment = false,
            Coefficient = 1m,
            CatalogKeyHash = "h2",
        };
        context.Equipments.AddRange(eqYes, eqNo);
        context.ProjectEquipments.AddRange(
            new ProjectEquipment
            {
                Id = 20,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                EquipmentId = 8,
                Quantity = 5m,
                FacilityCode = "OBJ-A",
                Equipment = eqYes,
            },
            new ProjectEquipment
            {
                Id = 21,
                ProjectId = 1,
                ProjectAnalysisVersionId = 10,
                EquipmentId = 9,
                Quantity = 4m,
                Equipment = eqNo,
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
}
