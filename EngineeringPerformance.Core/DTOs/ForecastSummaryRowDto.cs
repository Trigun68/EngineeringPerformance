namespace EngineeringPerformance.Core.DTOs;

public sealed record ForecastSummaryRowDto(
    string Code,
    string Name,
    string DisplayName,
    string NormUnitName,
    decimal? FactPhysicalVolume,
    decimal? NormProductivity,
    decimal? BaseHours,
    decimal? OptimisticHours,
    decimal? PessimisticHours,
    bool IsTotal,
    int Level);
