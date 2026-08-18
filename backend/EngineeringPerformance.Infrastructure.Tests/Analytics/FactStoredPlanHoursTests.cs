using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Плановые часы на «Оценка факта» читаются из сохранённых колонок ФО, без повторного N(V).
/// </summary>
public sealed class FactStoredPlanHoursTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    private const int DirId = 20;

    private static IicWorkDirection Classifier() => new()
    {
        Id = DirId,
        Code = "IIC-3",
        Name = "Строительство",
        Unit = new UnitOfMeasurement { Id = 2, Name = "м²" },
    };

    private static PhysicalVolumeKfo Kfo() => new()
    {
        Id = 5,
        Code = "KQ.01",
        Name = "Площадь",
        IsAssessment = true,
        ClassifierId = DirId,
        Coefficient = 1m,
    };

    private static ProjectPhysicalVolume Ppv(
        int id,
        decimal quantity,
        string facility,
        decimal? pes,
        decimal? bas,
        decimal? opt,
        PhysicalVolumeKfo kfo) => new()
    {
        Id = id,
        Quantity = quantity,
        FacilityCode = facility,
        FacilityName = "Объект",
        DocumentSet = "ДС-1",
        PhysicalVolumeKfoId = kfo.Id,
        PhysicalVolumeKfo = kfo,
        LaborIntensityPessimistic = pes,
        LaborIntensityBase = bas,
        LaborIntensityOptimistic = opt,
    };

    private static ProjectsPageVersionBundle Bundle(
        IReadOnlyList<ProjectPhysicalVolume> ppvs,
        IReadOnlyList<ProjectEquipment>? equipment = null)
    {
        var classifier = Classifier();
        return new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes = ppvs,
            Equipment = equipment ?? [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>
            {
                [DirId] = new(
                    10m, null, 1m,
                    NormFormulaMapper.FromLegacyPerformance(10m),
                    NormFormulaMapper.FromLegacyStdDev(10m, 1m)),
            },
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [DirId] = classifier },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>(),
            NormIdByDirection = new Dictionary<int, int>(),
        };
    }

    private static PerformanceRowDto PerfRow(decimal? factHours = null) => new(
        DirId, "Строительство", "IIC-3", null, 0, "м²", 10m, factHours, null, null, 10m, null, 1m, null, null);

    [Fact]
    public void BuildFactGrids_Detail_UsesStoredPpvHours_NotVolumeTimesNorm()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(100, quantity: 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var row = detail.Should().ContainSingle(r => !r.IsEquipment).Subject;
        row.PessimisticHours.Should().Be(12m);
        row.BaseHours.Should().Be(50m, "сохранённые часы, а не 10×10 нормы");
        row.OptimisticHours.Should().Be(8m);
        row.FactLaborHours.Should().BeNull();
    }

    [Fact]
    public void BuildFactGrids_Equipment_HasNoPlanHours()
    {
        var kfo = Kfo();
        var eq = new ProjectEquipment
        {
            Id = 200,
            Quantity = 100m,
            FacilityCode = "01",
            FacilityName = "Объект",
            CatalogName = "Насос",
            Equipment = new Equipment
            {
                Id = 7,
                Name = "Насос",
                ClassifierId = DirId,
                IsAssessment = true,
                Coefficient = 1m,
            },
        };
        var bundle = Bundle([Ppv(100, 10m, "01", 12m, 50m, 8m, kfo)], [eq]);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var eqRow = detail.Should().ContainSingle(r => r.IsEquipment).Subject;
        eqRow.BaseHours.Should().BeNull();
        eqRow.OptimisticHours.Should().BeNull();
        eqRow.PessimisticHours.Should().BeNull();
    }

    [Fact]
    public void BuildFactGrids_Facility_SumsStoredPlanHoursByFacilityCode()
    {
        var kfo = Kfo();
        var bundle = Bundle(
        [
            Ppv(1, 2m, "A", pes: 3m, bas: 10m, opt: 1m, kfo),
            Ppv(2, 8m, "A", pes: 7m, bas: 40m, opt: 9m, kfo),
            Ppv(3, 4m, "B", pes: 5m, bas: 20m, opt: 2m, kfo),
        ]);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var a = facility.Should().ContainSingle(f => f.FacilityCode == "A").Subject;
        a.PessimisticHours.Should().Be(10m);
        a.BaseHours.Should().Be(50m);
        a.OptimisticHours.Should().Be(10m);

        var b = facility.Should().ContainSingle(f => f.FacilityCode == "B").Subject;
        b.BaseHours.Should().Be(20m);
    }

    [Fact]
    public void AttachStoredPlanHours_ThenTotals_RollUpScenarios()
    {
        var kfo = Kfo();
        var parent = new PerformanceRowDto(1, "Root", "R", null, 0, "м²", 10m, 4m, null, null, null, null, null, null, null);
        var leaf = new PerformanceRowDto(DirId, "Leaf", "L", 1, 1, "м²", 10m, 4m, null, null, null, null, null, null, null);
        var attached = ProjectsPageGridBuilder.AttachStoredPlanHours(
            [parent, leaf],
            [Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);

        attached.Single(r => r.WorkDirectionId == DirId).BaseHours.Should().Be(50m);
        attached.Single(r => r.WorkDirectionId == 1).BaseHours.Should().BeNull();

        var withTotals = ProjectsPageGridBuilder.AppendPerformanceTotals(attached);
        var rootTotal = withTotals.Single(r => r.IsTotal && r.WorkDirectionName.StartsWith("Итого:"));
        rootTotal.PessimisticHours.Should().Be(12m);
        rootTotal.BaseHours.Should().Be(50m);
        rootTotal.OptimisticHours.Should().Be(8m);
        rootTotal.FactLaborHours.Should().Be(8m);

        var grand = withTotals.Single(r => r.WorkDirectionName == "ИТОГО");
        grand.BaseHours.Should().Be(50m);
        grand.FactLaborHours.Should().Be(8m);
    }

    [Fact]
    public void AttachStoredPlanHours_MapsOntoAnalysisDirectionDto()
    {
        var kfo = Kfo();
        var leaf = new PerformanceRowDto(
            DirId, "Leaf", "L", null, 0, "м²", 10m, 4m, null, null, null, null, null, null, null);
        var attached = ProjectsPageGridBuilder.AttachStoredPlanHours(
            [leaf],
            [Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var row = attached.Should().ContainSingle().Subject;

        var dto = new ProjectVersionDirectionDto(
            Id: row.WorkDirectionId,
            ParentId: row.ParentId,
            Level: row.Level,
            Code: row.WorkDirectionCode,
            Name: row.WorkDirectionName,
            UnitName: row.NormUnitName,
            FactPhysicalVolume: row.FactPhysicalVolume,
            FactLaborHours: row.FactLaborHours,
            PessimisticHours: row.PessimisticHours,
            BaseHours: row.BaseHours,
            OptimisticHours: row.OptimisticHours,
            FactProductivity: row.FactProductivity,
            FactEstimate: row.FactEstimate,
            NormProductivity: row.NormProductivity,
            NormEstimate: row.NormEstimate,
            NormStdDev: row.NormStdDev,
            ZScore: row.ZScore,
            Interpretation: row.Interpretation,
            PhysicalVolumes: [],
            EquipmentVolumes: [],
            DirectionLaborIntensities: []);

        dto.PessimisticHours.Should().Be(12m);
        dto.BaseHours.Should().Be(50m);
        dto.OptimisticHours.Should().Be(8m);
        dto.FactLaborHours.Should().Be(4m);
    }
}
