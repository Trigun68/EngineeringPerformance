using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class PlanningScenarioReportMetricsTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ComputePlanningWeightedZ_UsesLeafBaseHours_LikePlanningDialog()
    {
        var bundle = TreeBundle(
            leafAHours: 10m,
            leafBHours: 30m,
            leafA: new PlanningZParameters(0.5m, 1m, -0.5m),
            leafB: new PlanningZParameters(1.5m, 3m, -1.5m));

        var (zPes, zBase, zOpt) = PlanningScenarioReportMetrics.ComputePlanningWeightedZ(bundle, _engine);

        // (1*10 + 3*30) / 40 = 2.5
        zBase.Should().Be(2.5m);
        // (0.5*10 + 1.5*30) / 40 = 1.25
        zPes.Should().Be(1.25m);
        // (−0.5*10 + −1.5*30) / 40 = −1.25
        zOpt.Should().Be(-1.25m);
    }

    [Fact]
    public void ComputePlanningWeightedZ_FallsBackToArithmeticMean_WhenLeafHoursAreZero()
    {
        var bundle = TreeBundle(
            leafAHours: 0m,
            leafBHours: 0m,
            leafA: new PlanningZParameters(0.5m, 1m, -0.5m),
            leafB: new PlanningZParameters(1.5m, 3m, -1.5m));

        var (_, zBase, _) = PlanningScenarioReportMetrics.ComputePlanningWeightedZ(bundle, _engine);

        zBase.Should().Be(2.00m);
    }

    [Fact]
    public void Compute_TakesScenarioHoursFromGrandTotals()
    {
        var bundle = TreeBundle(
            leafAHours: 10m,
            leafBHours: 30m,
            leafA: new PlanningZParameters(0.5m, 1m, -0.5m),
            leafB: new PlanningZParameters(1.5m, 3m, -1.5m));
        var totals = new ForecastScenarioTotalsDto(100m, 80m, 120m);

        var result = PlanningScenarioReportMetrics.Compute(bundle, _engine, totals);

        result.ZBase.Should().Be(2.5m);
        result.HoursBase.Should().Be(100m);
        result.HoursOptimistic.Should().Be(80m);
        result.HoursPessimistic.Should().Be(120m);
    }

    [Fact]
    public void Compute_DoesNotUseStampAsPerformanceZ()
    {
        var bundle = TreeBundle(
            leafAHours: 10m,
            leafBHours: 30m,
            leafA: new PlanningZParameters(0.5m, -0.86m, -0.5m),
            leafB: new PlanningZParameters(0.5m, -0.86m, -0.5m));

        var (_, zBase, _) = PlanningScenarioReportMetrics.ComputePlanningWeightedZ(bundle, _engine);

        zBase.Should().Be(-0.86m);
    }

    private static ProjectsPageVersionBundle TreeBundle(
        decimal leafAHours,
        decimal leafBHours,
        PlanningZParameters leafA,
        PlanningZParameters leafB)
    {
        var parent = Dir(1, null, "P", "Родитель");
        var a = Dir(3, 1, "A", "Лист A");
        var b = Dir(4, 1, "B", "Лист B");
        var kfoA = Kfo(11, 3);
        var kfoB = Kfo(12, 4);

        return new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [parent, a, b],
            PhysicalVolumes =
            [
                Ppv(21, kfoA, leafAHours),
                Ppv(22, kfoB, leafBHours),
            ],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
            ClassifierMap = new Dictionary<int, IicWorkDirection>
            {
                [1] = parent,
                [3] = a,
                [4] = b,
            },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>
            {
                [3] = leafA,
                [4] = leafB,
            },
            NormIdByDirection = new Dictionary<int, int>(),
        };
    }

    private static IicWorkDirection Dir(int id, int? parentId, string code, string name) =>
        new()
        {
            Id = id,
            ParentId = parentId,
            Code = code,
            Name = name,
        };

    private static PhysicalVolumeKfo Kfo(int id, int classifierId) =>
        new()
        {
            Id = id,
            Code = $"K{id}",
            Name = $"K{id}",
            IsAssessment = true,
            ClassifierId = classifierId,
            Coefficient = 1m,
        };

    private static ProjectPhysicalVolume Ppv(int id, PhysicalVolumeKfo kfo, decimal baseHours) =>
        new()
        {
            Id = id,
            Quantity = 1m,
            FacilityCode = "A",
            PhysicalVolumeKfoId = kfo.Id,
            PhysicalVolumeKfo = kfo,
            LaborIntensityBase = baseHours,
            LaborIntensityOptimistic = baseHours,
            LaborIntensityPessimistic = baseHours,
        };
}
