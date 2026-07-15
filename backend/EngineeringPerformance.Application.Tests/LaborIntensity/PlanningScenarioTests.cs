using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.LaborIntensity;

public sealed class PlanningScenarioTests
{
    private static NormCalculationSnapshot Snap(decimal a, decimal sigma) => new(
        a, null, a * sigma,
        NormFormulaMapper.FromLegacyPerformance(a),
        NormFormulaMapper.FromLegacyStdDev(a, a * sigma));

    [Fact]
    public void ByEachValue_ComputesPerRow()
    {
        var lines = new[] { new LaborIntensityCalculator.PpvLine(1, 10, "F1", 5m, 1m, true) };
        var norms = new Dictionary<int, NormCalculationSnapshot> { [10] = Snap(100m, 0.1m) };
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByEachValue, lines, norms, new Dictionary<int, Application.Analytics.PlanningZParameters>(),
            new DeviationCalculator(new NormCalculator()));
        results.Should().ContainSingle(r => r.Base == 500m);
    }

    [Fact]
    public void ByProjectIicGroup_DistributesProportionally()
    {
        var lines = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, 10, "F1", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, 10, "F2", 8m, 1m, true),
        };
        var norms = new Dictionary<int, NormCalculationSnapshot> { [10] = Snap(100m, 0m) };
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, lines, norms, new Dictionary<int, Application.Analytics.PlanningZParameters>(),
            new DeviationCalculator(new NormCalculator()));
        results.Sum(r => r.Base).Should().Be(1000m);
    }

    [Fact]
    public void ByObjectIicGroup_GroupsByFacility()
    {
        var lines = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, 10, "F1", 2m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, 10, "F2", 8m, 1m, true),
            new LaborIntensityCalculator.PpvLine(3, 10, "F1", 4m, 1m, true),
        };
        var norms = new Dictionary<int, NormCalculationSnapshot> { [10] = Snap(100m, 0m) };
        var results = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByObjectIicGroup, lines, norms, new Dictionary<int, Application.Analytics.PlanningZParameters>(),
            new DeviationCalculator(new NormCalculator()));

        results.Where(r => r.PpvId is 1 or 3).Sum(r => r.Base ?? 0).Should().Be(600m);
        results.Single(r => r.PpvId == 2).Base.Should().Be(800m);
    }

    [Fact]
    public void ByProjectIicGroup_WithCustomZ_AppliesZToGroupVolume()
    {
        var lines = new[]
        {
            new LaborIntensityCalculator.PpvLine(1, 10, "F1", 5m, 1m, true),
            new LaborIntensityCalculator.PpvLine(2, 10, "F2", 5m, 1m, true),
        };
        var norms = new Dictionary<int, NormCalculationSnapshot> { [10] = Snap(100m, 0.1m) };
        var zParams = new Dictionary<int, Application.Analytics.PlanningZParameters> { [10] = new(0.5m, 0.3m, -0.5m) };
        var calculator = new DeviationCalculator(new NormCalculator());

        var groupResults = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByProjectIicGroup, lines, norms, zParams, calculator);
        var eachResults = LaborIntensityCalculator.Recalculate(
            PlanningScenario.ByEachValue, lines, norms, zParams, calculator);

        groupResults.Sum(r => r.Base ?? 0).Should().BeApproximately(1030m, 0.01m);
        eachResults.Sum(r => r.Base ?? 0).Should().BeApproximately(1030m, 0.01m);
    }
}
