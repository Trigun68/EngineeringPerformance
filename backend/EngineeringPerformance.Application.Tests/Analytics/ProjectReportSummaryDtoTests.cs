using EngineeringPerformance.Application.Analytics;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Контракт полей источника сводки «план/факт» — без математики Z.
/// </summary>
public sealed class ProjectReportSummaryDtoTests
{
    [Fact]
    public void StoresPlanSourceDirectionAndPrimaryFlags()
    {
        var dto = new ProjectReportSummaryDto(
            PlanVersionId: 11,
            PlanVersionName: "Факт v5",
            PlanSourceDirection: 2,
            PlanIsPrimary: true,
            PlanHours: 18240m,
            PlanWeightedZScore: -0.18m,
            PlanPercentile: "30-40-й",
            PlanInterpretation: "Хорошо",
            FactVersionId: 11,
            FactVersionName: "Факт v5",
            FactIsPrimary: true,
            FactHours: 16980m,
            FactWeightedZScore: -0.34m,
            FactPercentile: "30-40-й",
            FactInterpretation: "Хорошо");

        dto.PlanSourceDirection.Should().Be(2);
        dto.PlanIsPrimary.Should().BeTrue();
        dto.FactIsPrimary.Should().BeTrue();
        dto.PlanVersionName.Should().Be("Факт v5");
    }

    [Fact]
    public void AllowsMissingFactAndNonPrimaryPlanSource()
    {
        var dto = new ProjectReportSummaryDto(
            PlanVersionId: 1,
            PlanVersionName: "План v1",
            PlanSourceDirection: 1,
            PlanIsPrimary: false,
            PlanHours: 100m,
            PlanWeightedZScore: 0.1m,
            PlanPercentile: "50-60-й",
            PlanInterpretation: null,
            FactVersionId: null,
            FactVersionName: null,
            FactIsPrimary: false,
            FactHours: null,
            FactWeightedZScore: null,
            FactPercentile: null,
            FactInterpretation: null);

        dto.FactVersionId.Should().BeNull();
        dto.PlanIsPrimary.Should().BeFalse();
        dto.PlanSourceDirection.Should().Be(1);
    }
}
