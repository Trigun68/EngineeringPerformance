using EngineeringPerformance.Application.Analytics;
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
