namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Тренд индекса производительности (% к норме) по основным версиям проектов.
/// Индекс считается только через IPerformanceCalculationEngine.ComputeProductivityIndex:
/// Σ(объём × норматив) / Σ(часы) × 100 по строкам с объёмом, нормой и часами.
/// </summary>
public sealed record ProductivityIndexTrendRequest(
    DateTime? PeriodStart = null,
    DateTime? PeriodEnd = null,
    int[]? ProjectIds = null);

public sealed record ProductivityIndexPointDto(
    DateTime Date,
    int VersionId,
    int ProjectId,
    string ProjectCode,
    string ProjectName,
    string VersionName,
    decimal? Index,
    decimal WeightHours,
    string Flag);

public sealed record ProductivityIndexGroupDto(
    int GroupId,
    string GroupName,
    IReadOnlyList<ProductivityIndexPointDto> Plan,
    IReadOnlyList<ProductivityIndexPointDto> Fact);

public sealed record ProductivityIndexTrendResponse(
    ProductivityIndexGroupDto Overall,
    IReadOnlyList<ProductivityIndexGroupDto> ByGroup,
    IReadOnlyList<string> Warnings,
    TrendDetailAggregationDto? Detail = null);

public interface IProductivityIndexTrendService
{
    Task<ProductivityIndexTrendResponse> GetProductivityIndexTrendAsync(
        ProductivityIndexTrendRequest request,
        CancellationToken cancellationToken = default);
}
