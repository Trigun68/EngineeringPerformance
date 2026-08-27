namespace EngineeringPerformance.Application.Projects.DTOs;

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
    /// <summary>Неархивные версии плана (Direction = Plan).</summary>
    int PlanVersionCount,
    /// <summary>Неархивные версии факта (Direction = Fact).</summary>
    int FactVersionCount,
    bool IsArchived);
