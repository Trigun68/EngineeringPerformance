using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Tests.Norms;

public sealed class FormulaTemplatesTests
{
    // Привязка к enum вместо жёсткого числа: тест не устаревает при добавлении формулы.
    [Fact]
    public void All_CoversEveryFormulaTypeWithNonNullMetadata()
    {
        var declaredTypes = Enum.GetValues<FormulaType>();

        Assert.Equal(declaredTypes.Length, FormulaTemplates.All.Count);
        Assert.All(FormulaTemplates.All, template => Assert.NotNull(template.DisplayName));
        Assert.All(declaredTypes, type => Assert.NotNull(FormulaTemplates.Get(type)));
    }
}
