using EngineeringPerformance.Application.Export;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Export;

public sealed class PdfNumberFormatTests
{
    [Fact]
    public void Hours_UsesOneDecimal_AndRuGrouping()
    {
        var hours = PdfNumberFormat.Hours(12345.67m)
            .Replace('\u00A0', ' ')
            .Replace('\u202F', ' ');
        hours.Should().Be("12 345,7");
        PdfNumberFormat.Volume(null).Should().Be("—");
        PdfNumberFormat.HoursWithUnit(10.04m).Should().EndWith(" ч");
    }

    [Fact]
    public void ProductivityAndZ_UseTwoDecimals()
    {
        PdfNumberFormat.Productivity(12.3456m).Should().Be("12,35");
        PdfNumberFormat.Z(-0.86m).Should().Be("-0,86");
        PdfNumberFormat.SignedZ(-0.86m).Should().Be("−0,86");
        PdfNumberFormat.SignedZ(1.2m).Should().Be("+1,20");
    }

    [Fact]
    public void ScenarioLabels_AreFullWords_InOptimisticBasePessimisticOrder()
    {
        PdfScenarioLabels.Optimistic.Should().Be("Оптимистичный");
        PdfScenarioLabels.Base.Should().Be("Базовый");
        PdfScenarioLabels.Pessimistic.Should().Be("Пессимистичный");
        PdfScenarioLabels.OptimisticA.Should().Be("Оптимистичный (версия А)");
        PdfScenarioLabels.PessimisticB.Should().Be("Пессимистичный (версия Б)");
    }
}
