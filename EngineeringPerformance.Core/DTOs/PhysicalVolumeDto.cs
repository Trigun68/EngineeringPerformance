namespace EngineeringPerformance.Core.DTOs;

public sealed record PhysicalVolumeDto(
    int Id,
    int? ParentId,
    string Code,
    string Name,
    string Description,
    int VolumeTypeId,
    string VolumeTypeName,
    int? ClassifierId,
    string? ClassifierName,
    string? DocumentSet,
    bool IsArchived);
