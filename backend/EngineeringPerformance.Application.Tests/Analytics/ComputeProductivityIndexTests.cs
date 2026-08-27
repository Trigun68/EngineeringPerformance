using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Application.Services;

namespace EngineeringPerformance.Application.Tests.Analytics;

/// <summary>
/// Индекс производительности (% к норме): Σ(объём × норматив) / Σ(часы) × 100.
/// Строка участвует, только когда известны объём, норма и часы одновременно.
/// </summary>
public sealed class ComputeProductivityIndexTests
{
    private readonly IPerformanceCalculationEngine _engine =
        new PerformanceCalculationEngine(new DeviationCalculator(new NormCalculator()));

    [Fact]
    public void ExactNormHours_Returns100()
    {
        // 100 ед × 2 ч/ед = 200 нормо-часов; факт 200 ч → ровно 100%.
        var rows = new[] { CreateRow(volume: 100m, norm: 2m, hours: 200m) };

        Assert.Equal(100.0m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void FasterThanNorm_ReturnsAbove100()
    {
        // 100 ед × 2 ч/ед = 200 н-ч; факт 160 ч → 125%.
        var rows = new[] { CreateRow(volume: 100m, norm: 2m, hours: 160m) };

        Assert.Equal(125.0m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void SlowerThanNorm_ReturnsBelow100()
    {
        // 100 ед × 2 ч/ед = 200 н-ч; факт 250 ч → 80%.
        var rows = new[] { CreateRow(volume: 100m, norm: 2m, hours: 250m) };

        Assert.Equal(80.0m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void MultipleRows_AggregatesSums()
    {
        // (100×2 + 50×4) / (200 + 150) = 400/350 ≈ 114.3%
        var rows = new[]
        {
            CreateRow(1, volume: 100m, norm: 2m, hours: 200m),
            CreateRow(2, volume: 50m, norm: 4m, hours: 150m),
        };

        Assert.Equal(114.3m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void RowsWithoutNormVolumeOrHours_AreExcluded()
    {
        var rows = new[]
        {
            CreateRow(1, volume: 100m, norm: 2m, hours: 200m),
            CreateRow(2, volume: null, norm: 2m, hours: 500m), // нет объёма — вне выборки
            CreateRow(3, volume: 100m, norm: null, hours: 500m), // нет нормы — вне выборки
            CreateRow(4, volume: 100m, norm: 2m, hours: null), // нет часов — вне обеих сумм
        };

        Assert.Equal(100.0m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void TotalAndSyntheticRows_AreSkipped()
    {
        var rows = new[]
        {
            CreateRow(1, volume: 100m, norm: 2m, hours: 200m),
            CreateRow(PerformanceRowDto.UnallocatedPhysicalVolumeWorkDirectionId, 1000m, 1m, 1000m),
            CreateRow(2, volume: 1000m, norm: 1m, hours: 1000m, isTotal: true),
        };

        Assert.Equal(100.0m, _engine.ComputeProductivityIndex(rows));
    }

    [Fact]
    public void NoCoveredRows_ReturnsNull()
    {
        var rows = new[] { CreateRow(volume: 100m, norm: null, hours: 200m) };

        Assert.Null(_engine.ComputeProductivityIndex(rows));
        Assert.Null(_engine.ComputeProductivityIndex([]));
    }

    private static PerformanceRowDto CreateRow(
        int workDirectionId = 1,
        decimal? volume = 100m,
        decimal? norm = 2m,
        decimal? hours = 200m,
        bool isTotal = false) =>
        new(
            workDirectionId,
            $"Dir {workDirectionId}",
            $"D{workDirectionId}",
            ParentId: null,
            Level: 0,
            NormUnitName: "ч",
            FactPhysicalVolume: volume,
            FactLaborHours: hours,
            FactProductivity: null,
            FactEstimate: null,
            NormProductivity: norm,
            NormEstimate: null,
            NormStdDev: null,
            ZScore: null,
            Interpretation: null,
            IsTotal: isTotal);
}
