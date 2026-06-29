using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

/// <summary>
/// Unit-тесты движка расчётов (без БД): Z-score по иерархии и распределение LI по DocSet.
/// </summary>
public sealed class PerformanceCalculationEngineTests
{
    private readonly IPerformanceCalculationEngine _engine = new PerformanceCalculationEngine();

    [Fact]
    public void CalculatePerformance_WithValidData_ReturnsCorrectZScoreAndLI()
    {
        // --- Иерархия IIC: корень 10 + листья 11/12 (как PerformanceServiceTests.SeedHierarchyAsync) ---
        var workDirections = new List<WorkDirectionCalculationNode>
        {
            new(10, null, "root", "R", null),
            new(11, 10, "leaf-A", "A", null),
            new(12, 10, "leaf-B", "B", null),
        };

        var physicalVolumes = new List<PhysicalVolumeCalculationLine>
        {
            new(11, true, 100m, 1m),
            new(12, true, 50m, 1m),
        };

        var laborIntensities = new List<LaborIntensityCalculationLine>
        {
            new(11, 200m),
            new(12, 150m),
        };

        var norms = new Dictionary<int, NormCalculationSnapshot>
        {
            [10] = new(2.5m, 1.0m, 0.5m),
        };

        var perfInput = new PerformanceCalculationInput(
            VersionExists: true,
            workDirections,
            physicalVolumes,
            Equipment: [],
            laborIntensities,
            norms);

        var rows = _engine.CalculatePerformance(perfInput);

        var parent = rows.Single(r => r.WorkDirectionId == 10);
        var leafA = rows.Single(r => r.WorkDirectionId == 11);
        var leafB = rows.Single(r => r.WorkDirectionId == 12);

        leafA.FactEstimate.Should().BeApproximately((decimal)Math.Log(2.0), 1e-6m);
        leafB.FactEstimate.Should().BeApproximately((decimal)Math.Log(3.0), 1e-6m);

        var expectedAgg = ((decimal)Math.Log(2.0) * 200m + (decimal)Math.Log(3.0) * 150m) / 350m;
        parent.FactEstimate.Should().NotBeNull();
        parent.FactEstimate!.Value.Should().BeApproximately(expectedAgg, 1e-6m);

        parent.NormEstimate.Should().Be(1.0m);
        parent.NormStdDev.Should().Be(0.5m);
        var expectedZ = (expectedAgg - 1.0m) / 0.5m;
        parent.ZScore.Should().NotBeNull();
        parent.ZScore!.Value.Should().BeApproximately(expectedZ, 1e-6m);

        // --- Распределение LI: factHours = setHours × (vol / setVol) ---
        var normByDirection = new Dictionary<int, PerformanceRowDto>
        {
            [11] = new(
                WorkDirectionId: 11,
                WorkDirectionName: "leaf-A",
                WorkDirectionCode: "A",
                ParentId: 10,
                Level: 1,
                NormUnitName: "ед.",
                FactPhysicalVolume: null,
                FactLaborHours: null,
                FactProductivity: null,
                FactEstimate: null,
                NormProductivity: 2m,
                NormEstimate: 1.0m,
                NormStdDev: 0.5m,
                ZScore: null,
                Interpretation: null),
        };

        var factPv = new List<FactPhysicalVolumeLine>
        {
            new("F1", "Facility 1", "DS1", "W1", "Work", "PV-A", "pv-a", 11, true, 50m, 1m),
            new("F1", "Facility 1", "DS1", "W2", "Work 2", "PV-A2", "pv-a2", 11, true, 50m, 1m),
        };

        var factLi = new List<FactLaborIntensityLine>
        {
            new("DS1", 11, 200m),
        };

        var factInput = new FactAssessmentCalculationInput(
            factPv,
            Equipment: [],
            factLi,
            normByDirection,
            new Dictionary<int, ClassifierDisplayNode>
            {
                [11] = new(11, "A", "leaf-A", "ед."),
            });

        var factResult = _engine.CalculateFactAssessment(factInput);

        var detail = factResult.Detail.First(r => r.PvCode == "PV-A");
        detail.FactLaborHours.Should().Be(100m, "200 × (50 / 100) по объёму комплекта DS1");
        detail.FactProductivity.Should().Be(2m, "setHours / setVol = 200 / 100");

        var factEstimate = (decimal)Math.Log(2.0);
        detail.ZScore.Should().BeApproximately(Math.Round((factEstimate - 1.0m) / 0.5m, 4), 1e-4m);
    }

    [Fact]
    public void ComputeForecastBaseHours_ElementMethod_UsesEstimate()
    {
        var hours = _engine.ComputeForecastBaseHours(10m, performance: 0.2m, estimate: 0.15m, laborHours: 99m, "element");
        hours.Should().Be(1.5m);
    }

    [Fact]
    public void ComputeForecastScenarioHours_WithStdDev_AppliesZScores()
    {
        var (opt, pes, range) = _engine.ComputeForecastScenarioHours(
            baseHours: 20m,
            performance: 0.2m,
            standardDeviation: 0.05m,
            plannedVolume: 100m,
            zOptimistic: -1m,
            zPessimistic: 1.5m);

        opt.Should().Be(15m);
        pes.Should().Be(27.5m);
        range.Should().Be("15.00 - 27.50");
    }
}
