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
        child.FactLaborHours.Should().Be(40m);
        child.FactPhysicalVolume.Should().Be(4m);
    }
}
