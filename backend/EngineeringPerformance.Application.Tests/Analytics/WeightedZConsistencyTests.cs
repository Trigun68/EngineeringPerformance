using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// ISS-010: одни и те же данные дают одинаковый взвешенный Z во всех представлениях,
/// потому что формула Σ(Z×вес)/Σ(вес) живёт только в <see cref="IPerformanceCalculationEngine"/>.
/// </summary>
public sealed class WeightedZConsistencyTests
{
    private static readonly (decimal? Z, decimal? Hours)[] Samples =
    [
        (1.0m, 50m),
        (-0.5m, 30m),
        (null, 20m),
        (2.0m, null),
    ];

    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void StreamingAndBatchAggregation_ProduceSameResult()
    {
        // Эталонная формула (те же правила, что у движка): без Z — пропуск; вес только > 0.
        decimal weightedSum = 0m;
        decimal weightSum = 0m;
        foreach (var (z, hours) in Samples)
        {
            if (z is not { } zVal)
                continue;
            if (hours is not { } w || w <= 0m)
                continue;

            weightedSum += zVal * w;
            weightSum += w;
        }

        var expected = weightSum > 0m
            ? Math.Round(weightedSum / weightSum, 4, MidpointRounding.ToEven)
            : (decimal?)null;

        var batch = _engine.ComputeWeightedZ(Samples.Select(s => (s.Z, s.Hours)), decimalPlaces: 4);

        // (1.0*50 + (-0.5)*30) / 80 = 0.4375
        Assert.Equal(0.4375m, expected);
        Assert.Equal(expected, batch);
    }

    [Fact]
    public void RootPerformanceRows_MatchGenericAggregation()
    {
        var rows = new List<PerformanceRowDto>
        {
            CreateRoot(workDirectionId: 1, zScore: 1.0m, subtreeHours: 50m),
            CreateRoot(workDirectionId: 2, zScore: -0.5m, subtreeHours: 30m),
            CreateRoot(workDirectionId: 3, zScore: null, subtreeHours: 20m),
        };

        var fromRows = _engine.ComputeWeightedZFromPerformanceRows(rows, decimalPlaces: 4);
        var fromSamples = _engine.ComputeWeightedZ(
            rows.Select(r => (r.ZScore, r.SubtreeHours)),
            decimalPlaces: 4);

        Assert.Equal(fromSamples, fromRows);
    }

    [Fact]
    public void ZeroWeights_ReturnNullOrArithmeticMeanByRequest()
    {
        (decimal? Z, decimal? Weight)[] zeroWeighted = [(1.0m, 0m), (3.0m, null)];

        Assert.Null(_engine.ComputeWeightedZ(zeroWeighted));
        Assert.Equal(2.0m, _engine.ComputeWeightedZ(zeroWeighted, arithmeticFallback: true));
    }

    [Fact]
    public void EmptySample_ReturnsNull()
    {
        Assert.Null(_engine.ComputeWeightedZ([], arithmeticFallback: true));
    }

    private static PerformanceRowDto CreateRoot(int workDirectionId, decimal? zScore, decimal? subtreeHours) =>
        new(
            workDirectionId,
            $"Dir {workDirectionId}",
            $"D{workDirectionId}",
            null,
            0,
            "ч",
            100m,
            50m,
            null,
            null,
            2m,
            null,
            null,
            zScore,
            null,
            SubtreeHours: subtreeHours);
}
