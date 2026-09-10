using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class ExclusiveClaimedPerformanceTests
{
    private static readonly PerformanceCalculationEngine Engine =
        new(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ClaimedChild_IsExcludedFromParentVolumeAndHours()
    {
        var directions = new[]
        {
            new WorkDirectionCalculationNode(1, null, "Бетон", "IIC-1", "м3"),
            new WorkDirectionCalculationNode(11, 1, "Стены", "IIC-1-1", "м3"),
            new WorkDirectionCalculationNode(12, 1, "Перекрытия", "IIC-1-2", "м3"),
        };

        var input = new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections: directions,
            PhysicalVolumes:
            [
                new PhysicalVolumeCalculationLine(11, true, 4m, 1m),
                new PhysicalVolumeCalculationLine(12, true, 6m, 1m),
            ],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(1, 100m),
                new LaborIntensityCalculationLine(11, 40m),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>(),
            RollHoursToFirstLevel: true,
            ClaimedWorkDirectionIds: new HashSet<int> { 1, 11 });

        var rows = Engine.CalculatePerformance(input);
        var parent = rows.Single(r => r.WorkDirectionId == 1);
        var child = rows.Single(r => r.WorkDirectionId == 11);

        parent.FactLaborHours.Should().Be(100m);
        parent.FactPhysicalVolume.Should().Be(6m);
        parent.PhysicalVolume.Should().BeNull();
        parent.AssessmentPhysicalVolume.Should().Be(6m);
        child.FactLaborHours.Should().Be(40m);
        child.FactPhysicalVolume.Should().Be(4m);
        child.PhysicalVolume.Should().Be(4m);
        child.AssessmentPhysicalVolume.Should().Be(4m);

        var unclaimedLeaf = rows.Single(r => r.WorkDirectionId == 12);
        unclaimedLeaf.PhysicalVolume.Should().Be(6m);
        unclaimedLeaf.FactPhysicalVolume.Should().BeNull();
        unclaimedLeaf.AssessmentPhysicalVolume.Should().BeNull();
    }

    [Fact]
    public void EmptyClaimedSet_StillRollsHoursToFirstLevel()
    {
        var directions = new[]
        {
            new WorkDirectionCalculationNode(1, null, "Бетон", "IIC-1", "м3"),
            new WorkDirectionCalculationNode(12, 1, "Перекрытия", "IIC-1-2", "м3"),
        };

        var input = new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections: directions,
            PhysicalVolumes:
            [
                new PhysicalVolumeCalculationLine(12, true, 6m, 1m),
            ],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(12, 100m),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>(),
            RollHoursToFirstLevel: true,
            ClaimedWorkDirectionIds: new HashSet<int>());

        var rows = Engine.CalculatePerformance(input);
        var l1 = rows.Single(r => r.WorkDirectionId == 1);
        var leaf = rows.Single(r => r.WorkDirectionId == 12);
        l1.FactLaborHours.Should().Be(100m);
        l1.PhysicalVolume.Should().BeNull();
        l1.AssessmentPhysicalVolume.Should().Be(6m);
        leaf.FactLaborHours.Should().BeNull();
        leaf.PhysicalVolume.Should().Be(6m);
        leaf.AssessmentPhysicalVolume.Should().BeNull();
    }

    [Fact]
    public void CalculateFactAssessment_ClaimedChild_DoesNotShareParentCoarseHours()
    {
        var classifier = new Dictionary<int, ClassifierDisplayNode>
        {
            [1] = new(1, "IIC-1", "Бетон", "м3", null),
            [11] = new(11, "IIC-1-1", "Стены", "м3", 1),
            [12] = new(12, "IIC-1-2", "Перекрытия", "м3", 1),
        };

        var result = Engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            [
                new FactPhysicalVolumeLine("F11", "Стены", "DS-11", "W11", "Work", "PV-11", "стены", 11, true, 4m, 1m),
                new FactPhysicalVolumeLine("F12", "Перекрытия", "DS-12", "W12", "Work", "PV-12", "перекрытия", 12, true, 6m, 1m),
            ],
            [],
            [
                new FactLaborIntensityLine(string.Empty, 1, 100m),
                new FactLaborIntensityLine(string.Empty, 11, 40m),
            ],
            new Dictionary<int, PerformanceRowDto>(),
            classifier,
            new HashSet<int> { 1, 11 }));

        result.Detail.Single(r => r.PvCode == "PV-11").FactLaborHours.Should().Be(40m);
        result.Detail.Single(r => r.PvCode == "PV-12").FactLaborHours.Should().Be(100m);
    }
}
