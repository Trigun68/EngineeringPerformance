namespace EngineeringPerformance.Core.DTOs;

public sealed record IicWorkDirectionDto(
    int Id,
    int? ParentId,
    string Code,
    string Name,
    string Description,
    int? UnitId,
    string? UnitName,
    bool IsArchived);
