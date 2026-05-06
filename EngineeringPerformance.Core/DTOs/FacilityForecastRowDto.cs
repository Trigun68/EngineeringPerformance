namespace EngineeringPerformance.Core.DTOs;

public sealed record FacilityForecastRowDto(
    string FacilityCode,
    string FacilityName,
    decimal? BaseHours,
    decimal? OptimisticHours,
    decimal? PessimisticHours);
