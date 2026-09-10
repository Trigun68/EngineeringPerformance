using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Факт без поэлементного ключа: часы на IIC 1-го уровня, P/Z от объёма поддерева,
/// объекты — по марке (DocumentSet) и группе ФО.
/// </summary>
public sealed class FactLaborCoarseAllocationTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    private const int L1 = 5;
    private const int L2 = 52;
    private const int L2Sibling = 53;

    private static NormCalculationSnapshot NormSnap(decimal productivity)
    {
        var estimate = PerformanceCalculationEngine.ProductivityToEstimate(productivity);
        return new NormCalculationSnapshot(
            productivity,
            estimate,
            1m,
            NormFormulaMapper.FromLegacyPerformance(productivity),
            NormFormulaMapper.FromLegacyStdDev(productivity, 1m));
    }

    private static PerformanceRowDto NormRow(int id, decimal productivity)
    {
        var estimate = PerformanceCalculationEngine.ProductivityToEstimate(productivity);
        return new(
            WorkDirectionId: id,
            WorkDirectionName: "g",
            WorkDirectionCode: "IIC",
            ParentId: id == L1 ? null : L1,
            Level: id == L1 ? 0 : 1,
            NormUnitName: "ед.",
            FactPhysicalVolume: null,
            FactLaborHours: null,
            FactProductivity: null,
            FactEstimate: null,
            NormProductivity: productivity,
            NormEstimate: estimate,
            NormStdDev: 1m,
            ZScore: null,
            Interpretation: null);
    }

    private static List<WorkDirectionCalculationNode> Tree() =>
    [
        new(L1, null, "Автоматизация", "IIC-5", "ед."),
        new(L2, L1, "АСУ", "IIC-5-2", "ед."),
    ];

    [Fact]
    public void CalculatePerformance_RollsHoursToFirstLevel_UsesSubtreeVolume()
    {
        var input = new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections: Tree(),
            PhysicalVolumes:
            [
                new PhysicalVolumeCalculationLine(L2, true, 10m, 1m),
            ],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(L2, 100m),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>
            {
                [L1] = NormSnap(10m),
                [L2] = NormSnap(10m),
            },
            RollHoursToFirstLevel: true);

        var rows = _engine.CalculatePerformance(input);
        var l1 = rows.Should().ContainSingle(r => r.WorkDirectionId == L1).Subject;
        l1.FactLaborHours.Should().Be(100m);
        l1.FactProductivity.Should().Be(10m);
        l1.ZScore.Should().NotBeNull();
        Math.Abs(l1.ZScore!.Value).Should().BeLessThan(0.01m);

        l1.SubtreePhysicalVolume.Should().Be(10m);
        l1.FactPhysicalVolume.Should().BeNull();
        l1.PhysicalVolume.Should().BeNull();
        l1.AssessmentPhysicalVolume.Should().Be(10m);

        var l2 = rows.Should().ContainSingle(r => r.WorkDirectionId == L2).Subject;
        l2.FactLaborHours.Should().BeNull();
        l2.FactPhysicalVolume.Should().Be(10m);
        l2.SubtreePhysicalVolume.Should().Be(10m);
        l2.PhysicalVolume.Should().Be(10m);
        l2.AssessmentPhysicalVolume.Should().BeNull();
    }

    [Fact]
    public void CalculatePerformance_WithoutRoll_KeepsHoursOnLeaf()
    {
        var input = new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections: Tree(),
            PhysicalVolumes:
            [
                new PhysicalVolumeCalculationLine(L2, true, 10m, 1m),
            ],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(L2, 100m),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>
            {
                [L2] = NormSnap(10m),
            },
            RollHoursToFirstLevel: false);

        var rows = _engine.CalculatePerformance(input);
        rows.Single(r => r.WorkDirectionId == L2).FactLaborHours.Should().Be(100m);
        rows.Single(r => r.WorkDirectionId == L1).FactLaborHours.Should().BeNull();
    }

    [Fact]
    public void CalculateFactAssessment_BrandMatchesDocumentSet_InL1Subtree()
    {
        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            [
                new FactPhysicalVolumeLine(
                    "4A0400", "Склад", "4A0000-ОП-ПЗ3", "W1", "Work",
                    "PV-A", "pv", L2Sibling, true, 10m, 1m),
            ],
            [],
            [new FactLaborIntensityLine("4A0000-ОП-ПЗ3", L2, 100m)],
            new Dictionary<int, PerformanceRowDto>
            {
                [L1] = NormRow(L1, 10m),
                [L2] = NormRow(L2, 10m),
                [L2Sibling] = NormRow(L2Sibling, 10m),
            },
            new Dictionary<int, ClassifierDisplayNode>
            {
                [L1] = new(L1, "IIC-5", "Автоматизация", "ед.", null),
                [L2] = new(L2, "IIC-5-2", "АСУ", "ед.", L1),
                [L2Sibling] = new(L2Sibling, "IIC-5-3", "КИП", "ед.", L1),
            }));

        result.Facility.Should().NotContain(f => f.Name == "(пусто)");
        result.Facility.Single(f => f.Code == "4A0400").FactLaborHours.Should().Be(100m);
        result.Detail.Single(r => r.PvCode == "PV-A").FactLaborHours.Should().Be(100m);
        result.Detail.Single(r => r.PvCode == "PV-A").ZScore.Should().NotBeNull();
    }

    [Fact]
    public void CalculateFactAssessment_NoBrand_SplitsAcrossFacilitiesInL1Subtree()
    {
        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            [
                new FactPhysicalVolumeLine("F1", "A", "DS1", "W1", "Work", "PV-1", "p1", L2, true, 40m, 1m),
                new FactPhysicalVolumeLine("F2", "B", "DS2", "W2", "Work", "PV-2", "p2", L2, true, 60m, 1m),
            ],
            [],
            [new FactLaborIntensityLine(string.Empty, L2, 100m)],
            new Dictionary<int, PerformanceRowDto>
            {
                [L1] = NormRow(L1, 10m),
                [L2] = NormRow(L2, 10m),
            },
            new Dictionary<int, ClassifierDisplayNode>
            {
                [L1] = new(L1, "IIC-5", "Автоматизация", "ед.", null),
                [L2] = new(L2, "IIC-5-2", "АСУ", "ед.", L1),
            }));

        result.Detail.Single(r => r.PvCode == "PV-1").FactLaborHours.Should().Be(40m);
        result.Detail.Single(r => r.PvCode == "PV-2").FactLaborHours.Should().Be(60m);
        result.Facility.Where(f => f.Code != string.Empty).Sum(f => f.FactLaborHours ?? 0m)
            .Should().Be(100m);
        result.Facility.Should().NotContain(f => f.Name == "(пусто)");
    }

    [Fact]
    public void CalculateFactAssessment_HoursWithoutWorkDirection_GoToEmpty()
    {
        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            [
                new FactPhysicalVolumeLine("F1", "A", "DS1", "W1", "Work", "PV-1", "p1", L2, true, 10m, 1m),
            ],
            [],
            [new FactLaborIntensityLine("DS1", null, 50m)],
            new Dictionary<int, PerformanceRowDto>(),
            new Dictionary<int, ClassifierDisplayNode>
            {
                [L2] = new(L2, "IIC-5-2", "АСУ", "ед.", L1),
            }));

        result.Facility.Should().ContainSingle(f => f.Name == "(пусто)");
        result.Facility.Single(f => f.Name == "(пусто)").FactLaborHours.Should().Be(50m);
        result.Detail.Single(r => r.PvCode == "PV-1").FactLaborHours.Should().BeNull();
    }

    [Fact]
    public void CalculateFactAssessment_ExactDocSetAndClassifier_DoesNotSpillToSibling()
    {
        var result = _engine.CalculateFactAssessment(new FactAssessmentCalculationInput(
            [
                new FactPhysicalVolumeLine("F1", "A", "DS1", "W1", "Work", "PV-exact", "p1", L2, true, 10m, 1m),
                new FactPhysicalVolumeLine("F2", "B", "DS1", "W2", "Work", "PV-sib", "p2", L2Sibling, true, 10m, 1m),
            ],
            [],
            [new FactLaborIntensityLine("DS1", L2, 80m)],
            new Dictionary<int, PerformanceRowDto>
            {
                [L1] = NormRow(L1, 10m),
                [L2] = NormRow(L2, 10m),
                [L2Sibling] = NormRow(L2Sibling, 10m),
            },
            new Dictionary<int, ClassifierDisplayNode>
            {
                [L1] = new(L1, "IIC-5", "Автоматизация", "ед.", null),
                [L2] = new(L2, "IIC-5-2", "АСУ", "ед.", L1),
                [L2Sibling] = new(L2Sibling, "IIC-5-3", "КИП", "ед.", L1),
            }));

        result.Detail.Single(r => r.PvCode == "PV-exact").FactLaborHours.Should().Be(80m);
        result.Detail.Single(r => r.PvCode == "PV-sib").FactLaborHours.Should().BeNull();
        result.Facility.Should().NotContain(f => f.Name == "(пусто)");
    }

    [Fact]
    public void CalculatePerformance_RolledHours_ProductivityIndexUsesSubtreeVolume()
    {
        var input = new PerformanceCalculationInput(
            VersionExists: true,
            WorkDirections: Tree(),
            PhysicalVolumes:
            [
                new PhysicalVolumeCalculationLine(L2, true, 10m, 1m),
            ],
            Equipment: [],
            LaborIntensities:
            [
                new LaborIntensityCalculationLine(L2, 100m),
            ],
            NormsByDirection: new Dictionary<int, NormCalculationSnapshot>
            {
                [L1] = NormSnap(10m),
                [L2] = NormSnap(10m),
            },
            RollHoursToFirstLevel: true);

        var rows = _engine.CalculatePerformance(input);
        _engine.ComputeProductivityIndex(rows).Should().Be(100.0m);
    }
}
