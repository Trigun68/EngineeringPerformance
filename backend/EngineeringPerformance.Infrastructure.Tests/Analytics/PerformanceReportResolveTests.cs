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
    public void ApplyLivePlanHoursToDirections_StampsZBase_AndRollsUpParent()
    {
        var engine = new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));
        var live = new Dictionary<int, (decimal? Base, decimal? Optimistic, decimal? Pessimistic)>
        {
            [2] = (100m, 80m, 120m),
            [3] = (50m, 40m, 60m),
        };
        var planning = new Dictionary<int, PlanningZParameters>
        {
            [1] = new(0.5m, 0m, -0.5m),
            [2] = new(0.5m, -0.4m, -0.5m),
            [3] = new(0.5m, 0.2m, -0.5m),
        };
        var rows = new List<PerformanceReportDirectionRowDto>
        {
            Dir(1, null, 0, volume: 15m),
            Dir(2, 1, 1, volume: 10m),
            Dir(3, 1, 1, volume: 5m),
        };

        var result = PerformanceReportService.ApplyLivePlanHoursToDirections(rows, live, planning, engine);

        var leafA = result.Single(r => r.WorkDirectionId == 2);
        leafA.FactLaborHours.Should().Be(100m);
        leafA.ZScore.Should().Be(-0.4m);
        leafA.SubtreeHours.Should().Be(100m);

        var leafB = result.Single(r => r.WorkDirectionId == 3);
        leafB.ZScore.Should().Be(0.2m);

        var root = result.Single(r => r.WorkDirectionId == 1);
        root.SubtreeHours.Should().Be(150m);
        root.ZScore.Should().Be(engine.ComputeWeightedZ(
            [(-0.4m, 100m), (0.2m, 50m)],
            decimalPlaces: FactLaborHoursTotals.WeightedZDecimalPlaces));
        root.ZScore.Should().NotBeNull();
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

    private static PerformanceReportDirectionRowDto Dir(
        int id,
        int? parentId,
        int level,
        decimal volume) =>
        new(
            WorkDirectionId: id,
            ParentId: parentId,
            Level: level,
            Code: $"C{id}",
            Name: $"N{id}",
            UnitName: "шт",
            FactPhysicalVolume: volume,
            FactLaborHours: null,
            SubtreeHours: null,
            FactProductivity: null,
            NormProductivity: 1m,
            NormStdDev: 0.1m,
            ZScore: null,
            Interpretation: null);
}
