using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Registers;

public sealed class WorkCostCascadeTests
{
    private static readonly WorkCostDirectionNode[] Tree =
    [
        new(1, null),
        new(2, 1),
        new(3, 2),
        new(4, 2),
        new(5, null),
    ];

    [Fact]
    public void Expand_Organization_WritesAllDirections()
    {
        var expanded = WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.Organization,
            [new WorkCostBatchItem(0, 1200m)]);

        expanded.Should().HaveCount(5);
        expanded.Values.Should().AllBeEquivalentTo(1200m);
    }

    [Fact]
    public void Expand_Level1_CascadesToDescendantsOnly()
    {
        var expanded = WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.Level1,
            [new WorkCostBatchItem(1, 50m)]);

        expanded.Keys.Should().BeEquivalentTo([1, 2, 3, 4]);
        expanded.Should().NotContainKey(5);
        expanded[3].Should().Be(50m);
    }

    [Fact]
    public void Expand_Level2_DeeperItemOverridesParentCascade()
    {
        var expanded = WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.Level2,
            [
                new WorkCostBatchItem(1, 10m),
                new WorkCostBatchItem(2, 20m),
            ]);

        expanded[1].Should().Be(10m);
        expanded[2].Should().Be(20m);
        expanded[3].Should().Be(20m);
        expanded[4].Should().Be(20m);
    }

    [Fact]
    public void Expand_EachElement_DoesNotCascade()
    {
        var expanded = WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.EachElement,
            [
                new WorkCostBatchItem(1, 10m),
                new WorkCostBatchItem(3, 30m),
            ]);

        expanded.Should().HaveCount(2);
        expanded[1].Should().Be(10m);
        expanded[3].Should().Be(30m);
    }

    [Fact]
    public void Expand_UnknownDirection_Throws()
    {
        var act = () => WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.EachElement,
            [new WorkCostBatchItem(99, 1m)]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*99*");
    }

    [Fact]
    public void Expand_NegativeRate_Throws()
    {
        var act = () => WorkCostCascade.Expand(
            Tree,
            WorkCostLevel.Organization,
            [new WorkCostBatchItem(0, -1m)]);

        act.Should().Throw<InvalidOperationException>();
    }
}
