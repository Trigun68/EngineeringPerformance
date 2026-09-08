using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Перерасход часов: факт − норматив × объём. Зеркало PdfInsightBuilder (без округления).
/// </summary>
public sealed class ComputeHoursOverrunTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void Overrun_FactMinusNormTimesVolume()
    {
        _engine.ComputeHoursOverrun(100m, 2m, 40m).Should().Be(20m);
    }

    [Fact]
    public void Savings_WhenFactBelowNormHours_IsNegative()
    {
        _engine.ComputeHoursOverrun(50m, 2m, 40m).Should().Be(-30m);
    }

    [Fact]
    public void ExactNorm_ReturnsZero()
    {
        _engine.ComputeHoursOverrun(100m, 2.5m, 40m).Should().Be(0m);
    }

    [Fact]
    public void FractionalInputs_NotRounded()
    {
        // 10.125 − 1.25 × 3.2 = 6.125; Math.Round до 2 знаков дал бы то же, но 1/8 не трогаем.
        var actual = _engine.ComputeHoursOverrun(10.125m, 1.25m, 3.2m);
        actual.Should().Be(10.125m - 1.25m * 3.2m);
        actual.Should().Be(6.125m);
    }
}
