using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.LaborIntensity;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Norms;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Плановые часы на «Оценка факта» и в дереве — сохранённые LaborIntensity*.
/// Live N(V) на GET не вызывается.
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
    public void BuildFactGrids_Detail_UsesStoredHours_NotEngineFormula()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(100, quantity: 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };
        var engineWouldCompute = ExpectedHours(10m);

        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        var row = detail.Should().ContainSingle(r => !r.IsEquipment).Subject;
        row.BaseHours.Should().Be(50m);
        row.OptimisticHours.Should().Be(8m);
        row.PessimisticHours.Should().Be(12m);
        row.BaseHours.Should().NotBe(engineWouldCompute.Base);
        row.FactLaborHours.Should().BeNull();
    }

    [Fact]
    public void BuildFactGrids_Equipment_UsesStoredPlanHours()
    {
        var kfo = Kfo();
        var eq = new ProjectEquipment
        {
            Id = 200,
            Quantity = 100m,
            FacilityCode = "01",
            FacilityName = "Объект",
            CatalogName = "Насос",
            LaborIntensityBase = 77m,
            LaborIntensityOptimistic = 66m,
            LaborIntensityPessimistic = 88m,
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
        eqRow.BaseHours.Should().Be(77m);
        eqRow.OptimisticHours.Should().Be(66m);
        eqRow.PessimisticHours.Should().Be(88m);
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
        a.BaseHours.Should().Be(50m);

        var b = facility.Should().ContainSingle(f => f.FacilityCode == "B").Subject;
        b.BaseHours.Should().Be(20m);
    }

    [Fact]
    public void BuildFactGrids_Facility_EmptyWhenStoredLaborIntensityNull()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(1, 10m, "A", pes: null, bas: null, opt: null, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        facility.Should().NotContain(f => f.FacilityCode == "A" && (f.BaseHours ?? 0m) > 0m);
    }

    [Fact]
    public void AttachLivePlanHours_ThenTotals_RollUpStoredHours()
    {
        var kfo = Kfo();
        var parent = new PerformanceRowDto(1, "Root", "R", null, 0, "м²", 10m, 4m, null, null, null, null, null, null, null);
        var leaf = new PerformanceRowDto(DirId, "Leaf", "L", 1, 1, "м²", 10m, 4m, null, null, 10m, null, 1m, null, null);
        var bundle = Bundle([Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var attached = ProjectsPageGridBuilder.AttachLivePlanHours(
            [parent, leaf],
            bundle,
            _engine);

        attached.Single(r => r.WorkDirectionId == DirId).BaseHours.Should().Be(50m);
        attached.Single(r => r.WorkDirectionId == 1).BaseHours.Should().BeNull();

        var withTotals = ProjectsPageGridBuilder.AppendPerformanceTotals(attached);
        var rootTotal = withTotals.Single(r => r.IsTotal && r.WorkDirectionName.StartsWith("Итого:"));
        rootTotal.BaseHours.Should().Be(50m);
        rootTotal.PessimisticHours.Should().Be(12m);
        rootTotal.OptimisticHours.Should().Be(8m);
        rootTotal.FactLaborHours.Should().Be(8m);

        var grand = withTotals.Single(r => r.WorkDirectionName == "ИТОГО");
        grand.BaseHours.Should().Be(50m);
        grand.FactLaborHours.Should().Be(8m);
    }

    [Fact]
    public void AttachLivePlanHours_MapsStoredHoursOntoAnalysisDirectionDto()
    {
        var kfo = Kfo();
        var leaf = new PerformanceRowDto(
            DirId, "Leaf", "L", null, 0, "м²", 10m, 4m, null, null, 10m, null, 1m, null, null);
        var bundle = Bundle([Ppv(1, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
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

        dto.BaseHours.Should().Be(50m);
        dto.PessimisticHours.Should().Be(12m);
        dto.OptimisticHours.Should().Be(8m);
        dto.FactLaborHours.Should().Be(4m);
    }

    [Fact]
    public void BuildForecastGrids_GrandTotals_MatchFactFacilitySum_AfterPersist()
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
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, _engine);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, performance, _engine);
        var (facility, _) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        facility.Sum(f => f.BaseHours ?? 0m).Should().Be(forecast.GrandTotals.BaseHours);
        forecast.GrandTotals.BaseHours.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void GetGrids_AfterPersist_MatchEngineHours()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(100, 10m, "01", null, null, null, kfo)]);
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, _engine);
        var expected = ExpectedHours(10m);
        var performance = new List<PerformanceRowDto> { PerfRow() };

        var forecast = ProjectsPageGridBuilder.BuildForecastGrids(bundle, performance);
        var (_, detail) = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, _engine);

        forecast.GrandTotals.BaseHours.Should().Be(expected.Base);
        detail.Should().ContainSingle(r => !r.IsEquipment)
            .Which.BaseHours.Should().Be(expected.Base);
    }

    [Fact]
    public void GetGrids_DoNotCallEngineHourFormulas()
    {
        var kfo = Kfo();
        var bundle = Bundle([Ppv(100, 10m, "01", pes: 12m, bas: 50m, opt: 8m, kfo)]);
        var performance = new List<PerformanceRowDto> { PerfRow() };
        var spy = new SpyEngine(_engine);

        _ = ProjectsPageGridBuilder.BuildForecastGrids(bundle, performance, spy);
        _ = ProjectsPageGridBuilder.BuildFactGrids(bundle, performance, spy);
        _ = ProjectsPageGridBuilder.ComputeLivePlanHoursByDirection(bundle, spy);
        _ = ProjectsPageGridBuilder.AttachLivePlanHours(performance, bundle, spy);

        spy.RecalculateCalls.Should().Be(0);
        spy.PpvHoursCalls.Should().Be(0);
        spy.EquipmentHoursCalls.Should().Be(0);
        spy.AggregateHoursCalls.Should().Be(0);
    }

    private sealed class SpyEngine(IPerformanceCalculationEngine inner) : IPerformanceCalculationEngine
    {
        public int RecalculateCalls { get; private set; }
        public int PpvHoursCalls { get; private set; }
        public int EquipmentHoursCalls { get; private set; }
        public int AggregateHoursCalls { get; private set; }

        public IReadOnlyList<PerformanceRowDto> CalculatePerformance(PerformanceCalculationInput input) =>
            inner.CalculatePerformance(input);

        public FactAssessmentCalculationResult CalculateFactAssessment(FactAssessmentCalculationInput input) =>
            inner.CalculateFactAssessment(input);

        public decimal ComputeForecastBaseHours(
            decimal plannedVolume, decimal? performance, decimal? estimate, decimal laborHours, string method) =>
            inner.ComputeForecastBaseHours(plannedVolume, performance, estimate, laborHours, method);

        public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
            NormFormula normFormula, DeviationFormula deviationFormula, decimal volume, decimal effectiveVolume, PlanningZParameters z) =>
            inner.ComputeForecastHours(normFormula, deviationFormula, volume, effectiveVolume, z);

        public (decimal? Base, decimal? Optimistic, decimal? Pessimistic) ComputeForecastHours(
            decimal volume, decimal? productivity, decimal? stdDev, PlanningZParameters z) =>
            inner.ComputeForecastHours(volume, productivity, stdDev, z);

        public IReadOnlyList<LaborIntensityCalculator.RecalcResult> RecalculateLaborIntensities(
            PlanningScenario scenario,
            IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
            IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
            IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection)
        {
            RecalculateCalls++;
            return inner.RecalculateLaborIntensities(scenario, lines, normsByDirection, planningZByDirection);
        }

        public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputePpvForecastHours(
            PlanningScenario scenario,
            IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
            IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
            IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection)
        {
            PpvHoursCalls++;
            return inner.ComputePpvForecastHours(scenario, lines, normsByDirection, planningZByDirection);
        }

        public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> AggregateForecastHoursByDirection(
            IReadOnlyList<LaborIntensityCalculator.PpvLine> lines,
            IReadOnlyDictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ppvHoursById,
            IEnumerable<(int ClassifierId, decimal VolumeInIicUnits)> equipmentVolumes,
            IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
            IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection)
        {
            AggregateHoursCalls++;
            return inner.AggregateForecastHoursByDirection(
                lines, ppvHoursById, equipmentVolumes, normsByDirection, planningZByDirection);
        }

        public Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)> ComputeEquipmentForecastHours(
            IReadOnlyList<LaborIntensityCalculator.EquipmentLine> lines,
            IReadOnlyDictionary<int, NormCalculationSnapshot> normsByDirection,
            IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection)
        {
            EquipmentHoursCalls++;
            return inner.ComputeEquipmentForecastHours(lines, normsByDirection, planningZByDirection);
        }

        public decimal? ComputeWeightedZFromPerformanceRows(
            IReadOnlyList<PerformanceRowDto> performanceRows, int decimalPlaces = 2) =>
            inner.ComputeWeightedZFromPerformanceRows(performanceRows, decimalPlaces);

        public decimal? ComputeWeightedZ(
            IEnumerable<(decimal? ZScore, decimal? Weight)> samples,
            int decimalPlaces = 2,
            MidpointRounding rounding = MidpointRounding.ToEven,
            bool arithmeticFallback = false) =>
            inner.ComputeWeightedZ(samples, decimalPlaces, rounding, arithmeticFallback);

        public PlanningZParameters ResolvePlanningZ(
            IReadOnlyDictionary<int, PlanningZParameters> planningZByDirection, int workDirectionId) =>
            inner.ResolvePlanningZ(planningZByDirection, workDirectionId);
    }
}
