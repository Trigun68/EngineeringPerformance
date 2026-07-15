using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

/// <summary>Интеграционные тесты сборки гридов прогноза (Z per discipline).</summary>
public sealed class ProjectsPageGridBuilderTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    private static NormCalculationSnapshot Snap(decimal performance, decimal sigma) => new(
        performance, null, performance * sigma,
        NormFormulaMapper.FromLegacyPerformance(performance),
        NormFormulaMapper.FromLegacyStdDev(performance, performance * sigma));

    [Fact]
    public void BuildForecastGrids_WithCustomZBase_BaseHoursIncludeZ()
    {
        const int dirId = 11;
        var performance = new List<PerformanceRowDto>
        {
            new(
                dirId,
                "Leaf",
                "L",
                null,
                0,
                "ед.",
                100m,
                null,
                null,
                null,
                0.2m,
                null,
                0.04m,
                null,
                null),
        };

        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [],
            PhysicalVolumes = [],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
            ClassifierMap = new Dictionary<int, IicWorkDirection>(),
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>
            {
                [dirId] = new(0.5m, 0.3m, -0.5m),
            },
            NormIdByDirection = new Dictionary<int, int>(),
        };

        var grids = ProjectsPageGridBuilder.BuildForecastGrids(bundle, performance, _engine);

        var row = grids.Summary.Single(r => r.WorkDirectionId == dirId && !r.IsTotal);
        row.BaseHours.Should().Be(21m, "100 × (0.2 + 0.3 × 0.04)");
        row.BaseHours.Should().NotBe(20m);
        row.OptimisticHours.Should().Be(18m);
        row.PessimisticHours.Should().Be(22m);

        var itogo = grids.Summary.SingleOrDefault(r => r.DisplayName == "ИТОГО");
        if (itogo is not null)
        {
            itogo.BaseHours.Should().Be(grids.GrandTotals.BaseHours);
            itogo.OptimisticHours.Should().Be(grids.GrandTotals.OptimisticHours);
            itogo.PessimisticHours.Should().Be(grids.GrandTotals.PessimisticHours);
        }
    }

    [Fact]
    public void BuildForecastGrids_FacilityTotals_MatchGrandTotals()
    {
        const int dirId = 10;
        var kfo = new PhysicalVolumeKfo
        {
            Id = 1,
            Code = "KQ.01.001",
            Name = "Work",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };
        var ppv1 = new ProjectPhysicalVolume
        {
            Id = 1,
            Quantity = 5m,
            FacilityCode = "01",
            FacilityName = "Obj A",
            PhysicalVolumeKfoId = 1,
            PhysicalVolumeKfo = kfo,
        };
        var ppv2 = new ProjectPhysicalVolume
        {
            Id = 2,
            Quantity = 5m,
            FacilityCode = "02",
            FacilityName = "Obj B",
            PhysicalVolumeKfoId = 1,
            PhysicalVolumeKfo = kfo,
        };

        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [],
            PhysicalVolumes = [ppv1, ppv2],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot> { [dirId] = Snap(100m, 0m) },
            ClassifierMap = new Dictionary<int, IicWorkDirection>(),
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            NormIdByDirection = new Dictionary<int, int>(),
        };

        var grids = ProjectsPageGridBuilder.BuildForecastGrids(bundle, [], _engine);

        var facilityBase = grids.Facility.Sum(r => r.BaseHours ?? 0m);
        var facilityOpt = grids.Facility.Sum(r => r.OptimisticHours ?? 0m);
        var facilityPes = grids.Facility.Sum(r => r.PessimisticHours ?? 0m);

        facilityBase.Should().Be(grids.GrandTotals.BaseHours);
        facilityOpt.Should().Be(grids.GrandTotals.OptimisticHours);
        facilityPes.Should().Be(grids.GrandTotals.PessimisticHours);
    }
}
