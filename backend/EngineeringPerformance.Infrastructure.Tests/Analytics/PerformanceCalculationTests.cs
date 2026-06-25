using FluentAssertions;
using EngineeringPerformance.Infrastructure.Analytics;
using Xunit;

namespace EngineeringPerformance.Infrastructure.Tests.Analytics;

/// <summary>
/// Тесты для расчётов производительности — Z-score, агрегация, граничные случаи.
/// </summary>
public sealed class PerformanceCalculationTests
{
    [Theory]
    [InlineData(100, 10, 80, -2.0)] // 2 sigma ниже среднего
    [InlineData(100, 10, 100, 0)]   // ровно среднее
    [InlineData(100, 10, 120, 2.0)] // 2 sigma выше среднего
    public void CalculateZScore_VariousInputs_ReturnsExpected(double performance, double stdDev, double norm, double expectedZ)
    {
        var z = PerformancePercentileHelper.CalculateZScore(performance, stdDev, norm);

        z.Should().BeApproximately(expectedZ, 0.01);
    }

    [Fact]
    public void CalculateZScore_ZeroStdDev_ReturnsNull()
    {
        var z = PerformancePercentileHelper.CalculateZScore(100, 0, 100);

        z.Should().BeNull();
    }

    [Fact]
    public void CalculateZScore_NegativeStdDev_ReturnsNull()
    {
        var z = PerformancePercentileHelper.CalculateZScore(100, -5, 100);

        z.Should().BeNull();
    }

    [Theory]
    [InlineData(-2.0, 2.28)]   // 2.28-й перцентиль
    [InlineData(0, 50)]        // 50-й перцентиль
    [InlineData(2.0, 97.72)]   // 97.72-й перцентиль
    public void ZScoreToPercentile_StandardZScores_ReturnsExpectedPercentile(double zScore, double expectedPercentile)
    {
        var percentile = PerformancePercentileHelper.ZScoreToPercentile(zScore);

        percentile.Should().BeApproximately(expectedPercentile, 0.5);
    }

    [Fact]
    public void InterpretPerformance_VeryHighZScore_ReturnsExcellent()
    {
        var interpretation = PerformanceInterpretationHelper.InterpretPerformance(2.5);

        interpretation.Should().Be(PerformanceInterpretation.Excellent);
    }

    [Fact]
    public void InterpretPerformance_VeryLowZScore_ReturnsCritical()
    {
        var interpretation = PerformanceInterpretationHelper.InterpretPerformance(-2.5);

        interpretation.Should().Be(PerformanceInterpretation.Critical);
    }

    [Fact]
    public void InterpretPerformance_NullZScore_ReturnsNoData()
    {
        var interpretation = PerformanceInterpretationHelper.InterpretPerformance(null);

        interpretation.Should().Be(PerformanceInterpretation.NoData);
    }
}
