using EngineeringPerformance.Core;
using FluentAssertions;

namespace EngineeringPerformance.Tests;

public sealed class ComparisonAccuracyTests
{
    [Fact]
    public void FromValuePercent_Null_ReturnsNotApplicable()
    {
        ComparisonAccuracy.FromValuePercent(null).Should().Be(ComparisonAccuracy.NotApplicable);
    }

    [Theory]
    [InlineData(0.0, "VeryHigh")]
    [InlineData(6.99, "VeryHigh")]
    [InlineData(7.0, "High")]
    [InlineData(8.99, "High")]
    [InlineData(9.0, "Standard")]
    [InlineData(10.99, "Standard")]
    [InlineData(11.0, "Reduced")]
    [InlineData(99.9, "Reduced")]
    public void FromValuePercent_BoundaryValues_ReturnsExpectedCategory(double percent, string expectedKey)
    {
        var actual = ComparisonAccuracy.FromValuePercent((decimal)percent);

        var expected = expectedKey switch
        {
            "VeryHigh" => ComparisonAccuracy.VeryHigh,
            "High" => ComparisonAccuracy.High,
            "Standard" => ComparisonAccuracy.Standard,
            "Reduced" => ComparisonAccuracy.Reduced,
            _ => throw new ArgumentOutOfRangeException(nameof(expectedKey), expectedKey, null)
        };

        actual.Should().Be(expected);
    }

    [Fact]
    public void FromValuePercent_NegativeValue_FallsIntoVeryHighBucket()
    {
        // Граничное условие: формально отрицательного отклонения быть не должно,
        // но Helper не валидирует — в этом тесте фиксируем текущее поведение.
        ComparisonAccuracy.FromValuePercent(-1m).Should().Be(ComparisonAccuracy.VeryHigh);
    }
}
