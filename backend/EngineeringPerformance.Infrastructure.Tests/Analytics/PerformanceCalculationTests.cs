using FluentAssertions;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Тесты для расчётов производительности — Z-score, агрегация, граничные случаи.
/// </summary>
public sealed class PerformanceCalculationTests
{
    [Theory]
    [InlineData(null, "Н/Д")]
    [InlineData(-1.2, "0-й")]
    [InlineData(-0.9, "0-10-й")]
    [InlineData(-0.7, "10-20-й")]
    [InlineData(-0.5, "20-30-й")]
    [InlineData(-0.3, "30-40-й")]
    [InlineData(-0.1, "40-50-й")]
    [InlineData(0.1, "50-60-й")]
    [InlineData(0.3, "60-70-й")]
    [InlineData(0.5, "70-80-й")]
    [InlineData(0.7, "80-90-й")]
    [InlineData(0.9, "90-100-й")]
    [InlineData(1.2, "100-й")]
    public void GetPercentile_VariousRanges_ReturnsExpected(double? zScore, string expectedPercentile)
    {
        var normalized = zScore.HasValue ? (decimal?)Convert.ToDecimal(zScore.Value) : null;
        var percentile = PerformancePercentileHelper.GetPercentile(normalized);

        percentile.Should().Be(expectedPercentile);
    }

    [Fact]
    public void InterpretPerformance_VeryHighZScore_ReturnsCritical()
    {
        var interpretation = PerformanceInterpretationHelper.GetInterpretation(2.5m);

        interpretation.Should().Be(PerformanceInterpretation.Critical);
    }

    [Fact]
    public void InterpretPerformance_VeryLowZScore_ReturnsExcellent()
    {
        var interpretation = PerformanceInterpretationHelper.GetInterpretation(-2.5m);

        interpretation.Should().Be(PerformanceInterpretation.Excellent);
    }

    [Fact]
    public void InterpretPerformance_NullZScore_ReturnsNull()
    {
        var interpretation = PerformanceInterpretationHelper.GetInterpretation(null);

        interpretation.Should().BeNull();
    }
}
