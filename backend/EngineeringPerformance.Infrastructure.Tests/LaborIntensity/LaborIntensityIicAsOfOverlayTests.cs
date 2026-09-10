using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.LaborIntensityServices;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.LaborIicBindings;

public sealed class LaborIntensityIicAsOfOverlayTests
{
    [Fact]
    public void ApplyToLaborRows_OverwritesStoredDirectionFromSlice()
    {
        var row = new ProjectLaborIntensity
        {
            LaborIntensityId = 7,
            WorkDirectionId = 99,
        };
        var slice = new LaborIntensityIicAsOfSlice(
            new Dictionary<int, int?> { [7] = 11 },
            new HashSet<int> { 11 });

        LaborIntensityIicAsOf.ApplyToLaborRows([row], slice);

        row.WorkDirectionId.Should().Be(11);
    }

    [Fact]
    public void ApplyToLaborRows_ClearsDirectionWhenCardHasNoPeriod()
    {
        var row = new ProjectLaborIntensity
        {
            LaborIntensityId = 7,
            WorkDirectionId = 99,
        };
        var slice = new LaborIntensityIicAsOfSlice(
            new Dictionary<int, int?>(),
            new HashSet<int>());

        LaborIntensityIicAsOf.ApplyToLaborRows([row], slice);

        row.WorkDirectionId.Should().BeNull();
    }

    [Fact]
    public void ApplyToLaborRows_KeepsStoredDirectionWithoutLaborCard()
    {
        var row = new ProjectLaborIntensity
        {
            LaborIntensityId = null,
            WorkDirectionId = 99,
        };
        var slice = new LaborIntensityIicAsOfSlice(
            new Dictionary<int, int?>(),
            new HashSet<int>());

        LaborIntensityIicAsOf.ApplyToLaborRows([row], slice);

        row.WorkDirectionId.Should().Be(99);
    }

    [Fact]
    public void Overlay_CalculationUsesAsOfGroup_NotStoredSnapshot()
    {
        var row = new ProjectLaborIntensity
        {
            LaborIntensityId = 7,
            WorkDirectionId = 1,
            PlannedHours = 40m,
        };
        var slice = new LaborIntensityIicAsOfSlice(
            new Dictionary<int, int?> { [7] = 11 },
            new HashSet<int> { 1, 11 });
        LaborIntensityIicAsOf.ApplyToLaborRows([row], slice);

        var engine = new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));
        var rows = engine.CalculatePerformance(new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections:
            [
                new WorkDirectionCalculationNode(1, null, "Бетон", "IIC-1", "м3"),
                new WorkDirectionCalculationNode(11, 1, "Стены", "IIC-1-1", "м3"),
            ],
            PhysicalVolumes: [],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(row.WorkDirectionId, row.PlannedHours),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>(),
            RollHoursToFirstLevel: true,
            ClaimedWorkDirectionIds: slice.ClaimedWorkDirectionIds));

        rows.Single(r => r.WorkDirectionId == 11).FactLaborHours.Should().Be(40m);
        rows.Single(r => r.WorkDirectionId == 1).FactLaborHours.Should().BeNull();
    }
}
