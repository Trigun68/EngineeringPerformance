namespace EngineeringPerformance.Application.Projects.DTOs;

public sealed record CreateProjectRequest(
    string Code,
    string Name,
    string? WorkNumber = null,
    string? ContractNumber = null,
    string? CalendarPlanNumber = null,
    string? Type = null,
    string? Customer = null,
    string? Contractor = null,
    string? Gip = null,
    string? WorkStatus = null,
    DateOnly? WorkStart = null,
    string? Program = null);
