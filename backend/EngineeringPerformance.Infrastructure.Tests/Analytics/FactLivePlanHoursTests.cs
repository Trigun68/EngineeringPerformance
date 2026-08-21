using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Плановые часы на «Оценка факта» и в дереве — live N(V)×Z, как Forecast «ИТОГО».
/// Сохранённые LaborIntensity* не читаются.
/// </summary>
public sealed class FactLivePlanHoursTests
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

    private (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ExpectedHours(decimal volume) =>
        _engine.ComputeForecastHours(volume, 10m, 1m, PlanningZParameters.Default);

    [Fact]
    public void BuildFactGrids_Detail_UsesLiveHours_IgnoresStoredPpvHours()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(100, quantity: 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };
        var expected = ExpectedHours(10m);

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var row = detail.Should().ContainSingle(r => !r.IsEquipment).Subject;
        row.BaseHours.Should().Be(expected.Base);
        row.OptimisticHours.Should().Be(expected.Optimistic);
        row.PessimisticHours.Should().Be(expected.Pessimistic);
        row.BaseHours.Should().NotBe(50m);
        row.FactLaborHours.Should().BeNull();
    }

    [Fact]
    public void BuildFactGrids_Equipment_HasLivePlanHours()
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
        var expectedEq = ExpectedHours(100m);

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var eqRow = detail.Should().ContainSingle(r => r.IsEquipment).Subject;
        eqRow.BaseHours.Should().Be(expectedEq.Base);
        eqRow.OptimisticHours.Should().Be(expectedEq.Optimistic);
        eqRow.PessimisticHours.Should().Be(expectedEq.Pessimistic);
    }

    [Fact]
    public void BuildFactGrids_Facility_SumsLivePlanHoursByFacilityCode()
    {
        var kfo = Kfo();
        var bundle = Bundle(
        [
            Ppv(1, 2m, "A", pes: 3m, bas: 10m, opt: 1m, kfo),
            Ppv(2, 8m, "A", pes: 7m, bas: 40m, opt: 9m, kfo),
            Ppv(3, 4m, "B", pes: 5m, bas: 20m, opt: 2m, kfo),
        ]);
        var performance = new List<PerformanceRowDto> { PerfRow() };
        var expectedA = ExpectedHours(2m).Base + ExpectedHours(8m).Base;
        var expectedB = ExpectedHours(4m).Base;

        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var a = facility.Should().ContainSingle(f => f.FacilityCode == "A").Subject;
        a.BaseHours.Should().Be(expectedA);
        a.BaseHours.Should().NotBe(50m);

        var b = facility.Should().ContainSingle(f => f.FacilityCode == "B").Subject;
        b.BaseHours.Should().Be(expectedB);
    }

    [Fact]
    public void BuildFactGrids_Facility_FilledWhenStoredLaborIntensityNull()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(1, 10m, "A", pes: null, bas: null, opt: null, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };
        var expected = ExpectedHours(10m);

        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        facility.Should().ContainSingle(f => f.FacilityCode == "A")
            .Which.BaseHours.Should().Be(expected.Base);
    }

    [Fact]
    public void AttachLivePlanHours_ThenTotals_RollUpScenarios()
    {
        var kfo = Kfo();
        var parent = new PerformanceRowDto(1, "Root", "R", null, 0, "м²", 10m, 4m, null, null, null, null, null, null, null);
        var leaf = new PerformanceRowDto(DirId, "Leaf", "L", 1, 1, "м²", 10m, 4m, null, null, 10m, null, 1m, null, null);
        var bundle = Bundle([Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var expected = ExpectedHours(10m);
        var attached = ProjectsPageGridBuilder.AttachLivePlanHours(
            [parent, leaf],
            bundle,
            _engine);

        attached.Single(r => r.WorkDirectionId == DirId).BaseHours.Should().Be(expected.Base);
        attached.Single(r => r.WorkDirectionId == 1).BaseHours.Should().BeNull();

        var withTotals = ProjectsPageGridBuilder.AppendPerformanceTotals(attached);
        var rootTotal = withTotals.Single(r => r.IsTotal && r.WorkDirectionName.StartsWith("Итого:"));
        rootTotal.BaseHours.Should().Be(expected.Base);
        rootTotal.PessimisticHours.Should().Be(expected.Pessimistic);
        rootTotal.OptimisticHours.Should().Be(expected.Optimistic);
        rootTotal.FactLaborHours.Should().Be(8m);

        var grand = withTotals.Single(r => r.WorkDirectionName == "ИТОГО");
        grand.BaseHours.Should().Be(expected.Base);
        grand.FactLaborHours.Should().Be(8m);
    }

    [Fact]
    public void AttachLivePlanHours_MapsOntoAnalysisDirectionDto()
    {
        var kfo = Kfo();
        var leaf = new PerformanceRowDto(
            DirId, "Leaf", "L", null, 0, "м²", 10m, 4m, null, null, 10m, null, 1m, null, null);
        var bundle = Bundle([Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var expected = ExpectedHours(10m);
        var attached = ProjectsPageGridBuilder.AttachLivePlanHours(
            [leaf],
            bundle,
            _engine);
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

        dto.BaseHours.Should().Be(expected.Base);
        dto.PessimisticHours.Should().Be(expected.Pessimistic);
        dto.OptimisticHours.Should().Be(expected.Optimistic);
        dto.FactLaborHours.Should().Be(4m);
        dto.BaseHours.Should().NotBe(50m);
    }

    [Fact]
    public void BuildForecastGrids_GrandTotals_MatchFactLiveFacilitySum()
    {
        var kfo = Kfo();
        var eq = new ProjectEquipment
        {
            Id = 200,
            Quantity = 5m,
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
        var bundle = Bundle([Ppv(100, 10m, "01", null, null, null, kfo)], [eq]);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, performance, _engine);
        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        facility.Sum(f => f.BaseHours ?? 0m).Should().Be(forecast.GrandTotals.BaseHours);
    }
}
