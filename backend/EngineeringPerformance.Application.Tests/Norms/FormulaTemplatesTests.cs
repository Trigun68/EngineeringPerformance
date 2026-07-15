using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class FormulaTemplatesTests
{
    [Fact]
    public void All_ContainsEightNonNullTemplates()
    {
        Assert.Equal(8, FormulaTemplates.All.Count);
        Assert.All(FormulaTemplates.All, t => Assert.NotNull(t.DisplayName));
    }
}
