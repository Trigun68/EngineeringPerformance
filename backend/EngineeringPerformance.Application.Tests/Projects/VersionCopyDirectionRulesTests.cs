using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EngineeringPerformance.Application.Tests.Projects;

public sealed class VersionCopyDirectionRulesTests
{
    [Fact]
    public void Resolve_NullTarget_KeepsSourceDirection()
    {
        VersionCopyDirectionRules.Resolve(AnalysisDirection.Plan, null)
            .Should().Be(AnalysisDirection.Plan);
        VersionCopyDirectionRules.Resolve(AnalysisDirection.Fact, null)
            .Should().Be(AnalysisDirection.Fact);
    }

    [Fact]
    public void Resolve_SameDirection_KeepsSource()
    {
        VersionCopyDirectionRules.Resolve(AnalysisDirection.Fact, AnalysisDirection.Fact)
            .Should().Be(AnalysisDirection.Fact);
    }

    [Fact]
    public void Resolve_PlanToFact_Allowed()
    {
        VersionCopyDirectionRules.Resolve(AnalysisDirection.Plan, AnalysisDirection.Fact)
            .Should().Be(AnalysisDirection.Fact);
    }

    [Fact]
    public void Resolve_FactToPlan_Throws()
    {
        var act = () => VersionCopyDirectionRules.Resolve(AnalysisDirection.Fact, AnalysisDirection.Plan);
        act.Should().Throw<ArgumentException>().WithMessage("*План → Факт*");
    }
}
