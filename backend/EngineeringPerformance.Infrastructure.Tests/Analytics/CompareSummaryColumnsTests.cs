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
                Interpretation: PerformanceInterpretation.Good),
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
                Interpretation: PerformanceInterpretation.Satisfactory),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perfA, perfB, bundleA, bundleB, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;

        row.FactPhysicalVolumeA.Should().Be(10m);
        row.FactPhysicalVolumeB.Should().Be(10m);
        row.FactLaborHoursA.Should().Be(20m);
        row.FactLaborHoursB.Should().Be(30m);
        row.FactProductivityA.Should().Be(2m);
        row.FactProductivityB.Should().Be(3m);
        row.FactZScoreA.Should().Be(1.5m);
        row.FactZScoreB.Should().Be(0.5m);
        row.FactZDelta.Should().Be(-1.0m);
        row.PlanZScoreA.Should().Be(0.2m);
        row.PlanZScoreB.Should().Be(-0.1m);
        row.PlanZDelta.Should().Be(-0.3m);
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

        var bundle = new ProjectsPageVersionBundle
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

        var perf = new List<PerformanceRowDto>
        {
            new(
                dirId,
                "Сценарии",
                "IIC-11",
                null,
                0,
                "м",
                FactPhysicalVolume: 10m,
                FactLaborHours: 5m,
                FactProductivity: 0.5m,
                FactEstimate: null,
                NormProductivity: 2m,
                NormEstimate: null,
                NormStdDev: 0.2m,
                ZScore: 0m,
                Interpretation: PerformanceInterpretation.Satisfactory),
        };

        var rows = ProjectsPageGridBuilder.BuildCompareSummary(perf, perf, bundle, bundle, _engine);
        var row = rows.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;

        row.PlannedLaborHoursA.Should().NotBeNull();
        row.PlannedLaborHoursOptimisticA.Should().NotBeNull();
        row.PlannedLaborHoursPessimisticA.Should().NotBeNull();
        row.PlannedLaborHoursOptimisticA.Should().NotBe(row.PlannedLaborHoursPessimisticA);
        row.PlannedProductivityA.Should().NotBeNull();
        row.PlannedProductivityOptimisticA.Should().NotBeNull();
        row.PlannedProductivityPessimisticA.Should().NotBeNull();
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
        row.PlanZScoreA.Should().Be(PlanningZParameters.Default.ZBase);
        row.PlanZDelta.Should().Be(0m);
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
