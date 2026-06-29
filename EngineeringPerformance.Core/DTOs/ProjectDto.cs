namespace EngineeringPerformance.Core.DTOs;

public sealed record ProjectDto(
    int Id,
    string Code,
    string Name,
    string? WorkNumber,
    string? ContractNumber,
    string? CalendarPlanNumber,
    string? Type,
    string? Customer,
    string? Contractor,
    string? Gip,
    string? WorkStatus,
    DateOnly? WorkStart,
    string? Program,
    int VersionCount);
