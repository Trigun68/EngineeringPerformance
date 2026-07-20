using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

/// <summary>
/// Сценарии планирования с нелинейной нормой Wright (кривая обучения).
/// При линейной N(V) суммы ByEachValue и ByProjectIicGroup совпадают — здесь проверяем различия.
/// </summary>
public sealed class PlanningScenarioLearningCurveTests
{
    private const int DirId = 9;

    private static readonly DeviationCalculator Calculator = new(new NormCalculator());

    private static NormCalculationSnapshot WrightSnap(decimal a = 100m, decimal b = 0.2m, decimal sigma = 0m)
    {
        var norm = new NormFormula
        {
            Type = FormulaType.Wright,
            MinVolume = 1,
            MaxVolume = 1000,
            Coefficients = new Dictionary<string, decimal> { ["a"] = a, ["b"] = b },
        };
        var dev = new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = sigma },
        };
        return new NormCalculationSnapshot(a, null, null, norm, dev);
    }

    private static decimal ForecastHours(decimal volume, NormCalculationSnapshot snap, PlanningZParameters? z = null)
    {
        var zParams = z ?? PlanningZParameters.Default;
        var (b, _, _) = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, volume, volume, zParams, Calculator);
        return b ?? 0m;
    }

    private static IReadOnlyList<LaborIntensityCalculator.PpvLine> StandardLines { get; } =
    [
        new(1, DirId, "OBJ-A", 2m, 1m, true),
        new(2, DirId, "OBJ-B", 8m, 1m, true),
        new(3, DirId, "OBJ-A", 4m, 1m, true),
    ];

    [Fact]
    public void Wright_ByEachValue_UsesPerRowVolume()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var expected = ForecastHours(2m, snap) + ForecastHours(8m, snap) + ForecastHours(4m, snap);
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByEachValue, StandardLines, norms, z, Calculator);

        results.Sum(r => r.Base ?? 0).Should().BeApproximately(expected, 0.02m);
        results.Should().HaveCount(3);
    }

    [Fact]
    public void Wright_ByProjectIicGroup_UsesAggregatedVolume_NotSumOfRows()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var eachTotal = LaborIntensityCalculator.Recalculate(
                PlanningScenario.ByEachValue, StandardLines, norms, z, Calculator)
            .Sum(r => r.Base ?? 0);

        var groupTotal = LaborIntensityCalculator.Recalculate(
                PlanningScenario.ByProjectIicGroup, StandardLines, norms, z, Calculator)
            .Sum(r => r.Base ?? 0);

        var expectedGroup = ForecastHours(14m, snap);

        groupTotal.Should().BeApproximately(expectedGroup, 0.02m);
        eachTotal.Should().NotBeApproximately(groupTotal, 0.5m,
            "Wright-норма: N(ΣV) · ΣV ≠ Σ N(V_i) · V_i");
    }

    [Fact]
    public void Wright_ByObjectIicGroup_GroupsByFacility()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, StandardLines, norms, z, Calculator);

        var objA = results.Where(r => r.PpvId is 1 or 3).Sum(r => r.Base ?? 0);
        var objB = results.Single(r => r.PpvId == 2).Base ?? 0;

        objA.Should().BeApproximately(ForecastHours(6m, snap), 0.02m);
        objB.Should().BeApproximately(ForecastHours(8m, snap), 0.02m);

        var projectTotal = LaborIntensityCalculator.Recalculate(
                PlanningScenario.ByProjectIicGroup, StandardLines, norms, z, Calculator)
            .Sum(r => r.Base ?? 0);

        (objA + objB).Should().NotBeApproximately(projectTotal, 0.5m,
            "группировка по объектам даёт другой суммарный LI, чем по всему проекту");
    }

    [Fact]
    public void ByProjectIicGroup_DistributesProportionally_ToGroupTotal()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var groupTotal = ForecastHours(14m, snap);
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, StandardLines, norms, z, Calculator);

        results.Single(r => r.PpvId == 1).Base.Should().BeApproximately(groupTotal * (2m / 14m), 0.02m);
        results.Single(r => r.PpvId == 2).Base.Should().BeApproximately(groupTotal * (8m / 14m), 0.02m);
        results.Single(r => r.PpvId == 3).Base.Should().BeApproximately(groupTotal * (4m / 14m), 0.02m);
        results.Sum(r => r.Base ?? 0).Should().BeApproximately(groupTotal, 0.02m);
    }

    [Fact]
    public void ByObjectIicGroup_ObjA_RowsShareObjectGroupTotal()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var objTotal = ForecastHours(6m, snap);
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, StandardLines, norms, z, Calculator);

        results.Single(r => r.PpvId == 1).Base.Should().BeApproximately(objTotal * (2m / 6m), 0.02m);
        results.Single(r => r.PpvId == 3).Base.Should().BeApproximately(objTotal * (4m / 6m), 0.02m);
    }

    [Fact]
    public void Wright_WithZ_AppliesZAtGroupLevel_ForByProjectIicGroup()
    {
        var snap = WrightSnap(sigma: 0.1m);
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var zParams = new Dictionary<int, PlanningZParameters>
        {
            [DirId] = new(0.5m, 0.3m, -0.5m),
        };

        var groupResults = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, StandardLines, norms, zParams, Calculator);
        var eachResults = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByEachValue, StandardLines, norms, zParams, Calculator);

        var expectedGroup = ForecastHours(14m, snap, zParams[DirId]);
        groupResults.Sum(r => r.Base ?? 0).Should().BeApproximately(expectedGroup, 0.05m);
        eachResults.Sum(r => r.Base ?? 0).Should().NotBeApproximately(expectedGroup, 0.5m);
    }
}
