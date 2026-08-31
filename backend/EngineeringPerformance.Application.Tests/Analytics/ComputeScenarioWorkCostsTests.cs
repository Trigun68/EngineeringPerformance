using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;
using FluentAssertions;

namespace EngineeringPerformance.Application.Tests.Analytics;

public sealed class ComputeScenarioWorkCostsTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void SumsHoursTimesRate_PerScenario_WithoutDoubleCountingParents()
    {
        var hours = new WorkCostHourSample[]
        {
            new(1, OptimisticHours: 10m, BaseHours: 20m, PessimisticHours: 30m),
            new(2, OptimisticHours: 2m, BaseHours: 4m, PessimisticHours: 6m),
        };
        var rates = new Dictionary<int, decimal> { [1] = 100m, [2] = 50m };

        var result = _engine.ComputeScenarioWorkCosts(hours, rates);

        result.Should().NotBeNull();
        result!.Optimistic.Should().Be(10m * 100m + 2m * 50m);
        result.Base.Should().Be(20m * 100m + 4m * 50m);
        result.Pessimistic.Should().Be(30m * 100m + 6m * 50m);
    }

    [Fact]
    public void SkipsDirectionsWithoutRate_AndUnallocatedIds()
    {
        var hours = new WorkCostHourSample[]
        {
            new(-1, 100m, 100m, 100m),
            new(3, 5m, 5m, 5m),
            new(4, 8m, 8m, 8m),
        };
        var rates = new Dictionary<int, decimal> { [4] = 10m };

        var result = _engine.ComputeScenarioWorkCosts(hours, rates);

        result.Should().NotBeNull();
        result!.Base.Should().Be(80m);
        result.Optimistic.Should().Be(80m);
        result.Pessimistic.Should().Be(80m);
    }

    [Fact]
    public void AggregatesDuplicateDirectionIds()
    {
        var hours = new WorkCostHourSample[]
        {
            new(7, 1m, 2m, 3m),
            new(7, 4m, 5m, 6m),
        };
        var rates = new Dictionary<int, decimal> { [7] = 2m };

        var result = _engine.ComputeScenarioWorkCosts(hours, rates);

        result!.Optimistic.Should().Be(10m);
        result.Base.Should().Be(14m);
        result.Pessimistic.Should().Be(18m);
    }

    [Fact]
    public void ReturnsNull_WhenNoDirectionHasBothHoursAndRate()
    {
        var hours = new WorkCostHourSample[]
        {
            new(1, 10m, 10m, 10m),
        };

        _engine.ComputeScenarioWorkCosts(hours, new Dictionary<int, decimal>())
            .Should().BeNull();
    }

    [Fact]
    public void TreatsNullHoursAsZero_WhenRateExists()
    {
        var hours = new WorkCostHourSample[]
        {
            new(1, OptimisticHours: null, BaseHours: 3m, PessimisticHours: null),
        };
        var rates = new Dictionary<int, decimal> { [1] = 10m };

        var result = _engine.ComputeScenarioWorkCosts(hours, rates);

        result.Should().NotBeNull();
        result!.Optimistic.Should().Be(0m);
        result.Base.Should().Be(30m);
        result.Pessimistic.Should().Be(0m);
    }
}
