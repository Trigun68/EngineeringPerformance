using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Core.Services;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class ProjectVersionAnalysisTotalsCalculatorTests
{
    [Fact]
    public void Calculate_TotalLaborHours_FromAllPlannedHours()
    {
        var rows = Array.Empty<PerformanceRowDto>();
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(rows, totalPlannedHours: 1234.5m, unallocatedPlannedHours: 0m);

        summary.TotalFactLaborHours.Should().Be(1234.5m);
        summary.UnallocatedLaborHours.Should().BeNull();
    }

    [Fact]
    public void Calculate_UnallocatedLaborHours_WhenPresent()
    {
        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(
            Array.Empty<PerformanceRowDto>(),
            totalPlannedHours: 500m,
            unallocatedPlannedHours: 42m);

        summary.UnallocatedLaborHours.Should().Be(42m);
    }

    [Fact]
    public void Calculate_WeightedZScore_UsesRootSubtreeHoursAsWeight()
    {
        var rows = new List<PerformanceRowDto>
        {
            MakeRoot(id: 1, z: 0.4m, subtreeHours: 100m),
            MakeRoot(id: 2, z: -0.2m, subtreeHours: 300m),
        };

        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(rows, 400m, 0m);

        // (0.4*100 + (-0.2)*300) / 400 = -0.05
        summary.WeightedZScore.Should().Be(-0.05m);
        summary.WeightedZScorePercentile.Should().Be("40-50-й");
        summary.WeightedZScoreInterpretation.Should().Be(PerformanceInterpretation.Satisfactory);
    }

    [Fact]
    public void Calculate_PhysicalVolumeTotals_GroupedByUnit()
    {
        var rows = new List<PerformanceRowDto>
        {
            MakeDirection(id: 10, unit: "м²", volume: 100m),
            MakeDirection(id: 11, unit: "м²", volume: 50m),
            MakeDirection(id: 12, unit: "шт.", volume: 7m),
        };

        var summary = ProjectVersionAnalysisTotalsCalculator.Calculate(rows, 0m, 0m);

        summary.PhysicalVolumeTotalsByUnit.Should().HaveCount(2);
        summary.PhysicalVolumeTotalsByUnit.Should().Contain(p => p.UnitName == "м²" && p.TotalVolume == 150m);
        summary.PhysicalVolumeTotalsByUnit.Should().Contain(p => p.UnitName == "шт." && p.TotalVolume == 7m);
    }

    private static PerformanceRowDto MakeRoot(int id, decimal z, decimal subtreeHours) =>
        new(id, $"Dir {id}", $"D{id}", null, 0, "ед.", null, null, null, null, null, null, null, z, null, subtreeHours);

    private static PerformanceRowDto MakeDirection(int id, string unit, decimal volume) =>
        new(id, $"Dir {id}", $"D{id}", 10, 1, unit, volume, null, null, null, null, null, null, null, null, null);
}
