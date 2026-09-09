using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

public sealed class PerformanceReportResolveTests
{
    [Fact]
    public void ResolveAssessmentVersionId_PrefersPrimaryFact_OverPrimaryPlan()
    {
        var versions = new List<ProjectAnalysisVersion>
        {
            Ver(1, AnalysisDirection.Plan, isPrimary: true, createdAt: new DateTime(2026, 1, 1)),
            Ver(2, AnalysisDirection.Fact, isPrimary: true, createdAt: new DateTime(2026, 2, 1)),
        };

        var id = PerformanceReportService.ResolveAssessmentVersionId(versions, null, null);
        id.Should().Be(2);
    }

    [Fact]
    public void ResolveAssessmentVersionId_FallsBackToPrimaryPlan_WhenNoPrimaryFact()
    {
        var versions = new List<ProjectAnalysisVersion>
        {
            Ver(1, AnalysisDirection.Plan, isPrimary: true, createdAt: new DateTime(2026, 1, 1)),
            Ver(2, AnalysisDirection.Fact, isPrimary: false, createdAt: new DateTime(2026, 2, 1)),
        };

        var id = PerformanceReportService.ResolveAssessmentVersionId(versions, null, null);
        id.Should().Be(1);
    }

    [Fact]
    public void ResolveAssessmentVersionId_UsesLatestFact_WhenNoPrimaries()
    {
        var versions = new List<ProjectAnalysisVersion>
        {
            Ver(1, AnalysisDirection.Plan, isPrimary: false, createdAt: new DateTime(2026, 1, 1)),
            Ver(2, AnalysisDirection.Fact, isPrimary: false, createdAt: new DateTime(2026, 1, 10)),
            Ver(3, AnalysisDirection.Fact, isPrimary: false, createdAt: new DateTime(2026, 2, 1)),
        };

        var id = PerformanceReportService.ResolveAssessmentVersionId(versions, null, null);
        id.Should().Be(3);
    }

    [Fact]
    public void ResolveVersionId_Fact_UsesPrimaryThenLatest()
    {
        var versions = new List<ProjectAnalysisVersion>
        {
            Ver(10, AnalysisDirection.Fact, isPrimary: false, createdAt: new DateTime(2026, 3, 1)),
            Ver(11, AnalysisDirection.Fact, isPrimary: true, createdAt: new DateTime(2026, 1, 1)),
        };

        PerformanceReportService.ResolveVersionId(versions, AnalysisDirection.Fact, null)
            .Should().Be(11);
    }

    [Fact]
    public void MergeDirections_TakesFactLaborFromFact_AndPlanLaborFromPlan()
    {
        var plan = new List<PerformanceReportDirectionRowDto>
        {
            Row(1, null, labor: 100, z: -0.5m, volume: 10),
        };
        var fact = new List<PerformanceReportDirectionRowDto>
        {
            Row(1, null, labor: 40, z: 0.2m, volume: 12),
        };

        var merged = PerformanceReportService.MergeDirections(plan, fact);
        merged.Should().ContainSingle();
        merged[0].PlanLaborHours.Should().Be(100);
        merged[0].FactLaborHours.Should().Be(40);
        merged[0].PhysicalVolume.Should().Be(12);
        merged[0].PlanZScore.Should().Be(-0.5m);
        merged[0].FactZScore.Should().Be(0.2m);
    }

    [Fact]
    public void BuildPlanPerformance_ComputesZFromLiveHours_NotZBaseStamp()
    {
        var engine = new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));
        const int dirId = 2;
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-2",
            Name = "Лист",
            Unit = new UnitOfMeasurement { Id = 1, Name = "шт" },
        };
        var kfo = new PhysicalVolumeKfo
        {
            Id = 5,
            Code = "K",
            Name = "K",
            IsAssessment = true,
            ClassifierId = dirId,
            Coefficient = 1m,
        };
        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes =
            [
                new ProjectPhysicalVolume
                {
                    Id = 10,
                    Quantity = 10m,
                    FacilityCode = "A",
                    PhysicalVolumeKfoId = kfo.Id,
                    PhysicalVolumeKfo = kfo,
                },
            ],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>
            {
                [dirId] = new(
                    10m, 0m, 1m,
                    NormFormulaMapper.FromLegacyPerformance(10m),
                    NormFormulaMapper.FromLegacyStdDev(10m, 1m)),
            },
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [dirId] = classifier },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>
            {
                [dirId] = new(0.5m, 0.2m, -0.5m),
            },
            NormIdByDirection = new Dictionary<int, int>(),
        };
        StoredPlanHoursTestHelper.ApplyEngineHours(bundle, engine);

        var plan = ProjectsPageGridBuilder.BuildPlanPerformance(bundle, engine);
        var leaf = plan.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;
        leaf.ZScore.Should().NotBe(0.2m);
        leaf.FactLaborHours.Should().BePositive();

        var liveHours = ProjectsPageGridBuilder.ComputeLivePlanHoursByDirection(bundle);
        var labor = new List<ProjectLaborIntensity>
        {
            new()
            {
                WorkDirectionId = dirId,
                PlannedHours = liveHours[dirId].Base ?? 0m,
            },
        };
        var factBundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes = bundle.PhysicalVolumes,
            Equipment = [],
            LaborIntensities = labor,
            NormsByDirection = bundle.NormsByDirection,
            ClassifierMap = bundle.ClassifierMap,
            PlanningZByDirection = bundle.PlanningZByDirection,
            NormIdByDirection = bundle.NormIdByDirection,
        };
        var fact = ProjectsPageGridBuilder.BuildPerformance(factBundle, engine);
        var factLeaf = fact.Should().ContainSingle(r => !r.IsTotal && r.WorkDirectionId == dirId).Subject;
        leaf.ZScore.Should().Be(factLeaf.ZScore);
    }

    [Fact]
    public void MapPlanDirectionRows_UsesZBaseStamp_NotLivePerformanceZ()
    {
        var engine = new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));
        const int dirId = 2;
        var classifier = new IicWorkDirection
        {
            Id = dirId,
            Code = "IIC-2",
            Name = "Лист",
            Unit = new UnitOfMeasurement { Id = 1, Name = "шт" },
        };
        var bundle = new ProjectsPageVersionBundle
        {
            VersionExists = true,
            WorkDirections = [classifier],
            PhysicalVolumes = [],
            Equipment = [],
            LaborIntensities = [],
            NormsByDirection = new Dictionary<int, NormCalculationSnapshot>(),
            ClassifierMap = new Dictionary<int, IicWorkDirection> { [dirId] = classifier },
            PlanningZByDirection = new Dictionary<int, PlanningZParameters>
            {
                [dirId] = new(0.5m, -0.86m, -0.5m),
            },
            NormIdByDirection = new Dictionary<int, int>(),
        };
        var liveRows = new List<PerformanceRowDto>
        {
            new(
                WorkDirectionId: dirId,
                WorkDirectionName: "Лист",
                WorkDirectionCode: "IIC-2",
                ParentId: null,
                Level: 0,
                NormUnitName: "шт",
                FactPhysicalVolume: 10m,
                FactLaborHours: 10m,
                FactProductivity: 1m,
                FactEstimate: null,
                NormProductivity: 10m,
                NormEstimate: null,
                NormStdDev: 1m,
                ZScore: 1.23m,
                Interpretation: null),
        };

        var mapped = PerformanceReportService.MapPlanDirectionRows(engine, bundle, liveRows);
        mapped.Should().ContainSingle();
        mapped[0].ZScore.Should().Be(-0.86m);
        mapped[0].ZScore.Should().NotBe(1.23m);
    }

    [Fact]
    public void FormatNormSnapshotKey_SortsPairs()
    {
        AnalysisVersionSnapshotService.FormatKey([(20, 2), (10, 5), (10, 1)])
            .Should().Be("10:1,10:5,20:2");
        AnalysisVersionSnapshotService.FormatKey([]).Should().BeNull();
    }

    [Fact]
    public void CreateCopies_ClonesWorkDirectionAndNormIds()
    {
        var copies = AnalysisVersionSnapshotService.CreateCopies([(4, 9), (5, 11)], targetVersionId: 77);
        copies.Should().HaveCount(2);
        copies.Should().OnlyContain(s => s.ProjectAnalysisVersionId == 77);
        copies.Select(s => (s.WorkDirectionId, s.NormId)).Should().Equal((4, 9), (5, 11));
    }

    private static ProjectAnalysisVersion Ver(
        int id,
        AnalysisDirection direction,
        bool isPrimary,
        DateTime createdAt) =>
        new()
        {
            Id = id,
            ProjectId = 1,
            Direction = direction,
            IsPrimary = isPrimary,
            CreatedAt = createdAt,
            VersionName = $"v{id}",
        };

    private static PerformanceReportDirectionRowDto Row(
        int id,
        int? parentId,
        decimal labor,
        decimal z,
        decimal volume) =>
        new(
            WorkDirectionId: id,
            ParentId: parentId,
            Level: 0,
            Code: "C",
            Name: "N",
            UnitName: "шт",
            FactPhysicalVolume: volume,
            FactLaborHours: labor,
            SubtreeHours: labor,
            FactProductivity: labor / volume,
            NormProductivity: 1m,
            NormStdDev: 0.1m,
            ZScore: z,
            Interpretation: null);
}
