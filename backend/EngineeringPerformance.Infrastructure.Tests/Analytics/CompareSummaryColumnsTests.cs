using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class CompareSummaryColumnsTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void BuildCompareSummary_MapsPlanAndFactMetrics_AndDeltas()
    {
        const int dirId = 42;
        var unit = new UnitOfMeasurement { Id = 1, Name = "м" };
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-1",
            Name = "Направление",
            Unit = unit,
        };

        var planningA = new Dictionary<int, PlanningZParameters>
        {
            [dirId] = new PlanningZParameters(0.5m, 0.2m, -0.5m),
        };
        var planningB = new Dictionary<int, PlanningZParameters>
        {
            [dirId] = new PlanningZParameters(0.5m, -0.1m, -0.5m),
        };

        var bundleA = EmptyBundle(classifier, planningA);
        var bundleB = EmptyBundle(classifier, planningB);

        var perfA = new List<PerformanceRowDto>
        {
            new(
                dirId,
                "Направление",
                "IIC-1",
                null,
                0,
                "м",
                FactPhysicalVolume: 10m,
                FactLaborHours: 20m,
                FactProductivity: 2m,
                FactEstimate: null,
                NormProductivity: 1m,
                NormEstimate: null,
                NormStdDev: 0.1m,
                ZScore: 1.5m,
                Interpretation: PerformanceInterpretation.Good,
                PhysicalVolume: 10m,
                AssessmentPhysicalVolume: 10m),
        };
        var perfB = new List<PerformanceRowDto>
        {
            new(
                dirId,
                "Направление",
                "IIC-1",
                null,
                0,
                "м",
                FactPhysicalVolume: 10m,
                FactLaborHours: 30m,
                FactProductivity: 3m,
                FactEstimate: null,
                NormProductivity: 1m,
                NormEstimate: null,
                NormStdDev: 0.1m,
                ZScore: 0.5m,
                Interpretation: PerformanceInterpretation.Satisfactory,
                PhysicalVolume: 10m,
                AssessmentPhysicalVolume: 10m),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perfA, perfB, bundleA, bundleB, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;

        row.FactPhysicalVolumeA.Should().Be(10m);
        row.FactPhysicalVolumeB.Should().Be(10m);
        row.PhysicalVolumeA.Should().Be(10m);
        row.PhysicalVolumeB.Should().Be(10m);
        row.AssessmentPhysicalVolumeA.Should().Be(10m);
        row.AssessmentPhysicalVolumeB.Should().Be(10m);
        row.FactLaborHoursA.Should().Be(20m);
        row.FactLaborHoursB.Should().Be(30m);
        row.FactProductivityA.Should().Be(2m);
        row.FactProductivityB.Should().Be(3m);
        row.FactZScoreA.Should().Be(1.5m);
        row.FactZScoreB.Should().Be(0.5m);
        row.FactZDelta.Should().Be(-1.0m);
        row.PlanZScoreA.Should().BeNull();
        row.PlanZScoreB.Should().BeNull();
        row.PlanZDelta.Should().BeNull();

        var itogo = rows.Should().ContainSingle(r => r.IsTotal && r.Name == "ИТОГО").Subject;
        itogo.FactLaborHoursA.Should().Be(20m);
        itogo.FactLaborHoursB.Should().Be(30m);
    }

    [Fact]
    public void BuildCompareSummary_MapsScenarioHourTriples()
    {
        const int dirId = 11;
        var unit = new UnitOfMeasurement { Id = 1, Name = "м" };
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-11",
            Name = "Сценарии",
            Unit = unit,
        };

        var planning = new Dictionary<int, PlanningZParameters>
        {
            [dirId] = new PlanningZParameters(-0.5m, 0m, 0.5m),
        };
        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };

        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes =
            [
                new ProjectPhysicalVolume
                {
                    Id = 10,
                    Quantity = 10m,
                    FacilityCode = "A",
                    PhysicalVolumeKfoId = kfo.Id,
                    PhysicalVolumeKfo = kfo,
                },
            ],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>
            {
                [dirId] = new(
                    2m, null, 0.2m,
                    NormFormulaMapper.FromLegacyPerformance(2m),
                    NormFormulaMapper.FromLegacyStdDev(2m, 0.2m)),
            },
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [classifier.Id] = classifier },
            PlanningZByDirection = planning,
            NormIdByDirection = new Dictionary<int, int>(),
        };
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, _engine);

        var perf = new List<PerformanceRowDto>
        {
            new(
                dirId,
                "Сценарии",
                "IIC-11",
                null,
                0,
                "м",
                FactPhysicalVolume: 999m,
                FactLaborHours: 5m,
                FactProductivity: 0.5m,
                FactEstimate: null,
                NormProductivity: 2m,
                NormEstimate: null,
                NormStdDev: 0.2m,
                ZScore: 0m,
                Interpretation: PerformanceInterpretation.Satisfactory,
                PhysicalVolume: 10m,
                AssessmentPhysicalVolume: 999m),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perf, perf, bundle, bundle, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;

        row.PlannedLaborHoursA.Should().NotBeNull();
        row.PlannedLaborHoursOptimisticA.Should().NotBeNull();
        row.PlannedLaborHoursPessimisticA.Should().NotBeNull();
        row.PlannedLaborHoursOptimisticA.Should().NotBe(row.PlannedLaborHoursPessimisticA);
        row.PlannedProductivityA.Should().Be(row.PlannedLaborHoursA / 10m);
        row.PlannedProductivityA.Should().NotBe(row.PlannedLaborHoursA / 999m);
        row.PlannedProductivityOptimisticA.Should().NotBeNull();
        row.PlannedProductivityPessimisticA.Should().NotBeNull();
    }

    [Fact]
    public void BuildCompareSummary_PlanZ_FromLiveHours_NotZBaseStamp()
    {
        const int dirId = 42;
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-1",
            Name = "Направление",
            Unit = new UnitOfMeasurement { Id = 1, Name = "м" },
        };
        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };
        var planning = new Dictionary<int, PlanningZParameters>
        {
            [dirId] = new(0.5m, 0.2m, -0.5m),
        };
        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes =
            [
                new ProjectPhysicalVolume
                {
                    Id = 10,
                    Quantity = 10m,
                    FacilityCode = "A",
                    PhysicalVolumeKfoId = kfo.Id,
                    PhysicalVolumeKfo = kfo,
                },
            ],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>
            {
                [dirId] = new(
                    10m, 0m, 1m,
                    NormFormulaMapper.FromLegacyPerformance(10m),
                    NormFormulaMapper.FromLegacyStdDev(10m, 1m)),
            },
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [dirId] = classifier },
            PlanningZByDirection = planning,
            NormIdByDirection = new Dictionary<int, int>(),
        };
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, _engine);
        var plan = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, _engine);
        var expectedZ = plan.Single(r => !r.IsTotal && r.WorkDirectionId == dirId).ZScore;
        var perf = new List<PerformanceRowDto>
        {
            new(dirId, "Направление", "IIC-1", null, 0, "м", 10m, 20m, 2m, null, 10m, null, 1m, 1.5m, null),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perf, perf, bundle, bundle, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;
        row.PlanZScoreA.Should().Be(expectedZ);
        row.PlanZScoreA.Should().NotBe(0.2m);
    }

    [Fact]
    public void BuildCompareSummary_PlannedProductivityNull_WhenNoVolume()
    {
        const int dirId = 7;
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-7",
            Name = "Без объёма",
            Unit = new UnitOfMeasurement { Id = 1, Name = "шт" },
        };
        var bundle = EmptyBundle(classifier, new Dictionary<int, PlanningZParameters>());
        var perf = new List<PerformanceRowDto>
        {
            new(dirId, "Без объёма", "IIC-7", null, 0, "шт", null, null, null, null, null, null, null, 0m, null),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perf, perf, bundle, bundle, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal).Subject;
        row.PlannedProductivityA.Should().BeNull();
        row.PlannedProductivityB.Should().BeNull();
        row.PlanZScoreA.Should().BeNull();
        row.PlanZDelta.Should().BeNull();
    }

    [Fact]
    public void BuildCompareSummary_Itogo_IncludesParentOwnHours_MatchesForecastGrandTotals()
    {
        const int parentId = 1;
        const int leafId = 20;
        var unit = new UnitOfMeasurement { Id = 1, Name = "м" };
        var parent = new IicWorkDirection { Id = parentId, Code = "IIC-P", Name = "Корень", Unit = unit };
        var leaf = new IicWorkDirection
        {
            Id = leafId,
            Code = "IIC-L",
            Name = "Лист",
            ParentId = parentId,
            Unit = unit,
        };
        var kfoParent = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "KP",
            Name = "P",
            IsAssessment = true,
            ClassifierId = parentId,
            Coefficient = 1m,
        };
        var kfoLeaf = new PhysicalVolumeKfo
        {
            Id = 6,
            Code = "KL",
            Name = "L",
            IsAssessment = true,
            ClassifierId = leafId,
            Coefficient = 1m,
        };
        var snap = new NormCalculationSnapshot(
            10m, null, 1m,
            NormFormulaMapper.FromLegacyPerformance(10m),
            NormFormulaMapper.FromLegacyStdDev(10m, 1m));
        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [parent, leaf],
            PhysicalVolumes =
            [
                new ProjectPhysicalVolume
                {
                    Id = 10,
                    Quantity = 5m,
                    FacilityCode = "A",
                    PhysicalVolumeKfoId = kfoParent.Id,
                    PhysicalVolumeKfo = kfoParent,
                },
                new ProjectPhysicalVolume
                {
                    Id = 11,
                    Quantity = 10m,
                    FacilityCode = "A",
                    PhysicalVolumeKfoId = kfoLeaf.Id,
                    PhysicalVolumeKfo = kfoLeaf,
                },
            ],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>
            {
                [parentId] = snap,
                [leafId] = snap,
            },
            ClassifierMap = new Dictionary<int, IicWorkDirection>
            {
                [parentId] = parent,
                [leafId] = leaf,
            },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            NormIdByDirection = new Dictionary<int, int>(),
        };
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, _engine);
        var perf = new List<PerformanceRowDto>
        {
            new(parentId, "Корень", "IIC-P", null, 0, "м", 5m, 4m, null, null, 10m, null, 1m, null, null),
            new(leafId, "Лист", "IIC-L", parentId, 1, "м", 10m, 8m, null, null, 10m, null, 1m, null, null),
        };

        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, perf);
        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perf, perf, bundle, bundle, _engine);
        var itogo = rows.Should().ContainSingle(r => r.IsTotal && r.Name == "ИТОГО").Subject;
        var leafRow = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == leafId).Subject;
        var parentRow = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == parentId).Subject;

        itogo.PlannedLaborHoursA.Should().Be(forecast.GrandTotals.BaseHours);
        itogo.PlannedLaborHoursOptimisticA.Should().Be(forecast.GrandTotals.OptimisticHours);
        itogo.PlannedLaborHoursPessimisticA.Should().Be(forecast.GrandTotals.PessimisticHours);
        itogo.FactLaborHoursA.Should().Be(12m);
        itogo.FactLaborHoursB.Should().Be(12m);
        parentRow.PlannedLaborHoursA.Should().BeGreaterThan(0m);
        leafRow.PlannedLaborHoursA.Should().BeGreaterThan(0m);
        itogo.PlannedLaborHoursA.Should().Be(
            (parentRow.PlannedLaborHoursA ?? 0m) + (leafRow.PlannedLaborHoursA ?? 0m));
        itogo.PlannedLaborHoursA.Should().NotBe(leafRow.PlannedLaborHoursA);

        var rootTotal = rows.Should().ContainSingle(r => r.IsTotal && r.Name.StartsWith("Итого:")).Subject;
        rootTotal.PlannedLaborHoursA.Should().Be(itogo.PlannedLaborHoursA);
        rootTotal.FactLaborHoursA.Should().Be(12m);
    }

    [Fact]
    public void BuildCompareGrids_SamePpvDifferentPli_PlannedHoursMatch_FactHoursDiffer()
    {
        const int dirId = 42;
        var unit = new UnitOfMeasurement { Id = 1, Name = "м" };
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-1",
            Name = "Направление",
            Unit = unit,
        };
        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };
        var snap = new NormCalculationSnapshot(
            10m, null, 1m,
            NormFormulaMapper.FromLegacyPerformance(10m),
            NormFormulaMapper.FromLegacyStdDev(10m, 1m));

        ProjectsPageVersionBundle MakeBundle(decimal plannedHours, int ppvId) => new()
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes =
            [
                new ProjectPhysicalVolume
                {
                    Id = ppvId,
                    Quantity = 10m,
                    FacilityCode = "A",
                    FacilityName = "Объект",
                    DocumentSet = "ДС-1",
                    PhysicalVolumeKfoId = kfo.Id,
                    PhysicalVolumeKfo = kfo,
                },
            ],
            Equipment = [],
            LaborIntensities =
            [
                new ProjectLaborIntensity
                {
                    ProjectId = 1,
                    DocumentationCode = "ДС-1",
                    WorkDirectionId = dirId,
                    PlannedHours = plannedHours,
                },
            ],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot> { [dirId] = snap },
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [dirId] = classifier },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            NormIdByDirection = new Dictionary<int, int>(),
        };

        var bundleA = MakeBundle(100m, 10);
        var bundleB = MakeBundle(250m, 11);
        StoredPlanHoursTestHelper.ApplyEngineHours(bundleA, _engine);
        StoredPlanHoursTestHelper.ApplyEngineHours(bundleB, _engine);
        var perfA = ProjectsPageGridBuilder.BuildPerformance(bundleA, _engine);
        var perfB = ProjectsPageGridBuilder.BuildPerformance(bundleB, _engine);
        var (facilityA, detailA) = ProjectsPageGridBuilder.BuildFactGrids(bundleA, perfA, _engine);
        var (facilityB, detailB) = ProjectsPageGridBuilder.BuildFactGrids(bundleB, perfB, _engine);
        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundleA, perfA);

        var grids = ProjectsPageGridBuilder.BuildCompareGrids(
            perfA,
            perfB,
            new ProjectsPageFactGridsDto(perfA, facilityA, detailA),
            new ProjectsPageFactGridsDto(perfB, facilityB, detailB),
            bundleA,
            bundleB,
            _engine);

        var itogo = grids.Summary.Should().ContainSingle(r => r.Name == "ИТОГО").Subject;
        itogo.PlannedLaborHoursA.Should().Be(forecast.GrandTotals.BaseHours);
        itogo.PlannedLaborHoursB.Should().Be(forecast.GrandTotals.BaseHours);
        itogo.PlannedLaborHoursA.Should().Be(itogo.PlannedLaborHoursB);
        itogo.FactLaborHoursA.Should().Be(100m);
        itogo.FactLaborHoursB.Should().Be(250m);

        grids.Facility.Sum(f => f.PlannedLaborHoursA ?? 0m).Should().Be(forecast.GrandTotals.BaseHours);
        grids.Facility.Sum(f => f.PlannedLaborHoursB ?? 0m).Should().Be(forecast.GrandTotals.BaseHours);
        facilityA.Sum(f => f.BaseHours ?? 0m).Should().Be(forecast.GrandTotals.BaseHours);
    }

    private static ProjectsPageVersionBundle EmptyBundle(
        IicWorkDirection classifier,
        IReadOnlyDictionary<int, PlanningZParameters> planning) =>
        new()
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes = [],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [classifier.Id] = classifier },
            PlanningZByDirection = planning,
            NormIdByDirection = new Dictionary<int, int>(),
        };
}
