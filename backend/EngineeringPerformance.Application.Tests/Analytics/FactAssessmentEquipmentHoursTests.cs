using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Факт. трудоёмкость: только из LI; оборудование не получает vol×NormProductivity.
/// </summary>
public sealed class FactAssessmentEquipmentHoursTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    private static PerformanceRowDto NormRow(int id, decimal normProductivity) => new(
        WorkDirectionId: id,
        WorkDirectionName: "leaf",
        WorkDirectionCode: "A",
        ParentId: null,
        Level: 0,
        NormUnitName: "ед.",
        FactPhysicalVolume: null,
        FactLaborHours: null,
        FactProductivity: null,
        FactEstimate: null,
        NormProductivity: normProductivity,
        NormEstimate: 1.0m,
        NormStdDev: 0.5m,
        ZScore: null,
        Interpretation: null);

    [Fact]
    public void CalculateFactAssessment_WithoutLaborIntensity_DoesNotInventEquipmentHours()
    {
        var normByDirection = new Dictionary<int, PerformanceRowDto>
        {
            [11] = NormRow(11, 2.5m),
        };

        var equipment = new List<FactEquipmentLine>
        {
            new("F1", "Facility 1", "DS1", "EQ-1", "Pump", 11, true, 100m, 1m),
        };

        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            PhysicalVolumes: [],
            equipment,
            LaborIntensities: [],
            normByDirection,
            new Dictionary<int, ClassifierDisplayNode>
            {
                [11] = new(11, "A", "leaf-A", "ед."),
            }));

        var eqRow = result.Detail.Should().ContainSingle(r => r.IsEquipment).Subject;
        eqRow.Quantity.Should().Be(100m);
        eqRow.FactLaborHours.Should().BeNull();
        eqRow.FactProductivity.Should().BeNull();
        eqRow.ZScore.Should().BeNull();

        result.Facility.Single(f => f.Code == "F1").FactLaborHours.Should().BeNull();
        result.Facility.Should().NotContain(f => f.Name == "(пусто)");
    }

    [Fact]
    public void CalculateFactAssessment_WithLaborIntensity_AllocatesOnlyToPhysicalVolumes()
    {
        var normByDirection = new Dictionary<int, PerformanceRowDto>
        {
            [11] = NormRow(11, 10m),
        };

        var factPv = new List<FactPhysicalVolumeLine>
        {
            new("F1", "Facility 1", "DS1", "W1", "Work", "PV-A", "pv-a", 11, true, 40m, 1m),
            new("F1", "Facility 1", "DS1", "W2", "Work 2", "PV-B", "pv-b", 11, true, 60m, 1m),
        };

        var equipment = new List<FactEquipmentLine>
        {
            new("F1", "Facility 1", "DS1", "EQ-1", "Pump", 11, true, 50m, 1m),
        };

        const decimal liHours = 200m;
        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            factPv,
            equipment,
            [new FactLaborIntensityLine("DS1", 11, liHours)],
            normByDirection,
            new Dictionary<int, ClassifierDisplayNode>
            {
                [11] = new(11, "A", "leaf-A", "ед."),
            }));

        result.Detail.Where(r => !r.IsEquipment && r.PvCode is "PV-A" or "PV-B")
            .Sum(r => r.FactLaborHours ?? 0m)
            .Should().Be(liHours);

        result.Detail.Single(r => r.IsEquipment).FactLaborHours.Should().BeNull();

        var facilityHours = result.Facility
            .Where(f => f.Code != string.Empty)
            .Sum(f => f.FactLaborHours ?? 0m);
        facilityHours.Should().Be(liHours);
        facilityHours.Should().BeLessThanOrEqualTo(liHours);
    }

    [Fact]
    public void CalculateFactAssessment_EmptyDocSet_DoesNotAllocateHours()
    {
        var normByDirection = new Dictionary<int, PerformanceRowDto>
        {
            [11] = NormRow(11, 10m),
        };

        var factPv = new List<FactPhysicalVolumeLine>
        {
            new("F1", "Facility 1", "DS1", "W1", "Work", "PV-A", "pv-a", 11, true, 40m, 1m),
        };

        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            factPv,
            [],
            [new FactLaborIntensityLine(string.Empty, 11, 200m)],
            normByDirection,
            new Dictionary<int, ClassifierDisplayNode>
            {
                [11] = new(11, "A", "leaf-A", "ед."),
            }));

        result.Detail.Single(r => r.PvCode == "PV-A").FactLaborHours.Should().BeNull();
        result.Facility.Where(f => f.Code != string.Empty).Sum(f => f.FactLaborHours ?? 0m)
            .Should().Be(0m);
    }
}
