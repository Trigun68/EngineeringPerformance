using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Analytics.FactNorm;
using EngineeringPerformance.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics.FactNorm;

public sealed class FactNormPointSelectorTests
{
    private const int L1 = 5;
    private const int L2 = 52;

    [Fact]
    public void Select_TakesFirstLevel_WithSubtreeVolume_SkipsLeaves()
    {
        var rows = new List<PerformanceRowDto>
        {
            Row(L1, null, 0, hours: 100m, ownVolume: null, subtreeVolume: 10m, productivity: 10m),
            Row(L2, L1, 1, hours: null, ownVolume: 10m, subtreeVolume: 10m, productivity: null),
        };

        var selected = FactNormPointSelector.Select(rows, requestedWorkDirectionIds: null);

        selected.Should().ContainSingle();
        selected[0].Row.WorkDirectionId.Should().Be(L1);
        selected[0].Volume.Should().Be(10m);
        selected[0].Productivity.Should().Be(10m);
    }

    [Fact]
    public void Select_MapsRequestedChildId_ToFirstLevel()
    {
        var rows = new List<PerformanceRowDto>
        {
            Row(L1, null, 0, hours: 100m, ownVolume: null, subtreeVolume: 10m, productivity: 10m),
            Row(L2, L1, 1, hours: null, ownVolume: 10m, subtreeVolume: 10m, productivity: null),
        };

        var selected = FactNormPointSelector.Select(rows, [L2]);

        selected.Should().ContainSingle(p => p.Row.WorkDirectionId == L1);
    }

    [Fact]
    public void Select_ReconstructsVolume_FromHoursAndProductivity()
    {
        var rows = new List<PerformanceRowDto>
        {
            Row(L1, null, 0, hours: 100m, ownVolume: null, subtreeVolume: null, productivity: 10m),
        };

        var selected = FactNormPointSelector.Select(rows, null);
        selected.Should().ContainSingle();
        selected[0].Volume.Should().Be(10m);
    }

    [Fact]
    public void ResolveSnapshotNormId_FallsBackToDescendantSnapshot()
    {
        var parentById = new Dictionary<int, int?> { [L1] = null, [L2] = L1 };
        var snapshot = new Dictionary<int, int> { [L2] = 99 };

        FactNormPointSelector.ResolveSnapshotNormId(L1, snapshot, parentById).Should().Be(99);
        FactNormPointSelector.ResolveSnapshotNormId(L1, new Dictionary<int, int> { [L1] = 7 }, parentById)
            .Should().Be(7);
    }

    [Fact]
    public void ForReportRow_UsesSubtreeOnRoot_OwnOnLeaf()
    {
        var root = Row(L1, null, 0, hours: 100m, ownVolume: null, subtreeVolume: 10m, productivity: 10m);
        var leaf = Row(L2, L1, 1, hours: null, ownVolume: 10m, subtreeVolume: 10m, productivity: null);

        PerformanceDisplayVolume.ForReportRow(root).Should().Be(10m);
        PerformanceDisplayVolume.ForReportRow(leaf).Should().Be(10m);
        PerformanceDisplayVolume.FirstLevel([root, leaf]).Should().ContainSingle(r => r.WorkDirectionId == L1);
    }

    private static PerformanceRowDto Row(
        int id,
        int? parentId,
        int level,
        decimal? hours,
        decimal? ownVolume,
        decimal? subtreeVolume,
        decimal? productivity) =>
        new(
            WorkDirectionId: id,
            WorkDirectionName: $"d{id}",
            WorkDirectionCode: $"C{id}",
            ParentId: parentId,
            Level: level,
            NormUnitName: "ед.",
            FactPhysicalVolume: ownVolume,
            FactLaborHours: hours,
            FactProductivity: productivity,
            FactEstimate: null,
            NormProductivity: 10m,
            NormEstimate: null,
            NormStdDev: 1m,
            ZScore: hours is > 0 ? 0m : null,
            Interpretation: hours is > 0 ? PerformanceInterpretation.Satisfactory : null,
            SubtreeHours: hours,
            SubtreePhysicalVolume: subtreeVolume);
}
