using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class PlannedVolumeDistributorTests
{
    [Fact]
    public void ByProject_ScalesAllRows_DoesNotZeroSiblings()
    {
        var qty = PlannedVolumeDistributor.Distribute(
            PlanningScenario.ByProjectIicGroup,
            20m,
            [(1m, 2m), (1m, 8m)]);

        qty.Should().Equal(4m, 16m);
    }

    [Fact]
    public void ByObject_PreservesObjectShares_ViaGlobalScale()
    {
        var qty = PlannedVolumeDistributor.Distribute(
            PlanningScenario.ByObjectIicGroup,
            30m,
            [(1m, 2m), (1m, 8m)]);

        qty.Should().Equal(6m, 24m);
    }

    [Fact]
    public void ByEachValue_SingleRow_WritesTarget()
    {
        var qty = PlannedVolumeDistributor.Distribute(
            PlanningScenario.ByEachValue,
            12m,
            [(2m, 1m)]);

        qty.Should().Equal(6m);
    }

    [Fact]
    public void ByEachValue_MultipleRows_Throws()
    {
        var act = () => PlannedVolumeDistributor.Distribute(
            PlanningScenario.ByEachValue,
            10m,
            [(1m, 2m), (1m, 8m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage(PlannedVolumeDistributor.EachValueMultipleRowsMessage);
    }

    [Fact]
    public void ZeroCurrentVolumes_SplitsEqually()
    {
        var qty = PlannedVolumeDistributor.Distribute(
            PlanningScenario.ByProjectIicGroup,
            10m,
            [(1m, 0m), (1m, 0m)]);

        qty.Should().Equal(5m, 5m);
    }
}
