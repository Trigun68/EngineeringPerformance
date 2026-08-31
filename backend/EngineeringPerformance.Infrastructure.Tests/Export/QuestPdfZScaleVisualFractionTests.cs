using EngineeringPerformance.Infrastructure.Export;
using FluentAssertions;

namespace EngineeringPerformance.Infrastructure.Tests.Export;

public sealed class QuestPdfZScaleVisualFractionTests
{
    [Theory]
    [InlineData(3, 0.0)]
    [InlineData(0, 0.5)]
    [InlineData(-3, 1.0)]
    public void ZScaleVisualFraction_Plus3Left_Minus3Right(double z, double expected)
    {
        QuestPdfReportComposer.ZScaleVisualFraction(z).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void ZScaleVisualFraction_ClampsOutsideRange()
    {
        QuestPdfReportComposer.ZScaleVisualFraction(10).Should().Be(0);
        QuestPdfReportComposer.ZScaleVisualFraction(-10).Should().Be(1);
    }

    [Fact]
    public void ZScaleVisualFraction_NarrowBand_WorseIsLeft()
    {
        QuestPdfReportComposer.ZScaleVisualFraction(2, min: -2, max: 2).Should().BeApproximately(0, 1e-9);
        QuestPdfReportComposer.ZScaleVisualFraction(-2, min: -2, max: 2).Should().BeApproximately(1, 1e-9);
    }
}
