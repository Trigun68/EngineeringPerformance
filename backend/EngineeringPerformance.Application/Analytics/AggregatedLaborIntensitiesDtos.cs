namespace EngineeringPerformance.Application.Analytics;

public enum AggregationLevel
{
    Total = 1,
    ByObject = 2,
}

public sealed record AggregatedLaborIntensityRowDto(
    string Key,
    string Label,
    decimal TotalBase,
    decimal TotalPessimistic,
    decimal TotalOptimistic);

public sealed record AggregatedLaborIntensitiesDto(
    AggregationLevel Level,
    IReadOnlyList<AggregatedLaborIntensityRowDto> Rows);
