using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

/// <summary>
/// Mixed V_g: assessed ФО и оборудование одной группы ИИС делят N(ΣV).
/// Wright зависит от V; логнормаль — нет (инвариант mixed = per-item).
/// </summary>
public sealed class MixedVolumeGroupingTests
{
    private const int DirId = 7;
    private static readonly DeviationCalculator Calculator = new(new NormCalculator());

    [Fact]
    public void Wright_ByProject_EquipmentRaisesVg_AndReducesPpvHours()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppv = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, DirId, "OBJ-A", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, DirId, "OBJ-B", 8m, 1m, true),
        };
        var equipment = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(20, DirId, 5m, true, "OBJ-A"),
        };

        var withoutEq = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, ppv, [], norms, z, Calculator);
        var mixed = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, ppv, equipment, norms, z, Calculator);

        var grouped15 = ForecastHours(15m, snap);
        mixed.Ppv.Sum(r => r.Base ?? 0m).Should().BeLessThan(withoutEq.Ppv.Sum(r => r.Base ?? 0m));
        (mixed.Ppv.Sum(r => r.Base ?? 0m) + (mixed.Equipment[20].Base ?? 0m))
            .Should().BeApproximately(grouped15, 0.02m);
        mixed.Equipment[20].Base.Should().Be(Math.Round(grouped15 * (5m / 15m), 2));
    }

    [Fact]
    public void Wright_ByObject_EquipmentMixesOnlyMatchingFacility()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppv = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, DirId, "OBJ-A", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, DirId, "OBJ-B", 8m, 1m, true),
        };
        var equipment = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(20, DirId, 5m, true, "OBJ-A"),
        };

        var mixed = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, ppv, equipment, norms, z, Calculator);

        var hoursA = ForecastHours(7m, snap);
        var hoursB = ForecastHours(8m, snap);
        mixed.Ppv.Single(r => r.PpvId == 1).Base.Should().Be(Math.Round(hoursA * (2m / 7m), 2));
        mixed.Ppv.Single(r => r.PpvId == 2).Base.Should().BeApproximately(hoursB, 0.02m);
        mixed.Equipment[20].Base.Should().Be(Math.Round(hoursA * (5m / 7m), 2));
    }

    [Fact]
    public void Wright_ByObject_EquipmentWithoutFacility_DoesNotMixWithPpv()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppv = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, DirId, "OBJ-A", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, DirId, "OBJ-B", 8m, 1m, true),
        };
        var equipment = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(20, DirId, 5m, true, null),
        };

        var ppvOnly = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, ppv, [], norms, z, Calculator);
        var mixed = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, ppv, equipment, norms, z, Calculator);

        mixed.Ppv.Single(r => r.PpvId == 1).Base.Should().Be(ppvOnly.Ppv.Single(r => r.PpvId == 1).Base);
        mixed.Ppv.Single(r => r.PpvId == 2).Base.Should().Be(ppvOnly.Ppv.Single(r => r.PpvId == 2).Base);
        mixed.Equipment[20].Base.Should().BeApproximately(ForecastHours(5m, snap), 0.02m);
    }

    [Fact]
    public void AssessmentNo_Equipment_ExcludedFromVg()
    {
        var snap = WrightSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppv = new[] { new LaborIntensityCalculator.PpvLine(1, DirId, "OBJ-A", 10m, 1m, true) };
        var equipment = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(20, DirId, 5m, false, "OBJ-A"),
        };

        var mixed = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, ppv, equipment, norms, z, Calculator);

        mixed.Ppv.Single().Base.Should().BeApproximately(ForecastHours(10m, snap), 0.02m);
        mixed.Equipment[20].Base.Should().BeNull();
    }

    [Fact]
    public void Kim_MixedEqualsPerItem()
    {
        var snap = KimSnap();
        var norms = new Dictionary<int, NormCalculationSnapshot> { [DirId] = snap };
        var z = new Dictionary<int, PlanningZParameters>();

        var ppv = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, DirId, "OBJ-A", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, DirId, "OBJ-B", 8m, 1m, true),
        };
        var equipment = new[]
        {
            new LaborIntensityCalculator.EquipmentLine(20, DirId, 5m, true, "OBJ-A"),
        };

        var each = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByEachValue, ppv, equipment, norms, z, Calculator);
        var project = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, ppv, equipment, norms, z, Calculator);

        each.Ppv.Sum(r => r.Base ?? 0m).Should().BeApproximately(project.Ppv.Sum(r => r.Base ?? 0m), 0.02m);
        each.Equipment[20].Base.Should().BeApproximately(project.Equipment[20].Base ?? 0m, 0.02m);
    }

    private static decimal ForecastHours(decimal volume, NormCalculationSnapshot snap)
    {
        var (b, _, _) = ForecastCalculation.ComputeForecastHours(
            snap.NormFormula, snap.DeviationFormula, volume, volume, PlanningZParameters.Default, Calculator);
        return b ?? 0m;
    }

    private static NormCalculationSnapshot WrightSnap() => new(
        100m, null, null,
        new NormFormula
        {
            Type = FormulaType.Wright,
            MinVolume = 1,
            MaxVolume = 1000,
            Coefficients = new Dictionary<string, decimal> { ["a"] = 100m, ["b"] = 0.2m },
        },
        new DeviationFormula
        {
            ModelType = DeviationModelType.Multiplicative,
            Coefficients = new Dictionary<string, decimal> { ["sigma"] = 0m },
        });

    private static NormCalculationSnapshot KimSnap()
    {
        var performance = 100m;
        var sigma = 0.1m;
        var mu = KimCurveMath.Ln(performance);
        return new NormCalculationSnapshot(
            performance, mu, sigma,
            new NormFormula
            {
                Type = FormulaType.KimCurve,
                MinVolume = 1,
                MaxVolume = 1000,
                Coefficients = new Dictionary<string, decimal>
                {
                    ["a"] = performance,
                    ["mu"] = mu,
                    ["sigma"] = sigma,
                },
            },
            new DeviationFormula
            {
                ModelType = DeviationModelType.Lognormal,
                Coefficients = new Dictionary<string, decimal> { ["sigma"] = sigma },
            });
    }
}
