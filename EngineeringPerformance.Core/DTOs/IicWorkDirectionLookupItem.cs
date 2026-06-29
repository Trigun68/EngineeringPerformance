namespace EngineeringPerformance.Core.DTOs;

public sealed record IicWorkDirectionLookupItem(
    int Id,
    int? ParentId,
    string Code,
    string Name);
