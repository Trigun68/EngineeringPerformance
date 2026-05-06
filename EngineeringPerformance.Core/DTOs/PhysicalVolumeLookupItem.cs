namespace EngineeringPerformance.Core.DTOs;

public sealed record PhysicalVolumeLookupItem(
    int Id,
    int? ParentId,
    string Code,
    string Name);
