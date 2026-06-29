namespace EngineeringPerformance.Core.DTOs;

public sealed record ProjectForecastAssessmentVolumesDto(
    IReadOnlyDictionary<int, List<ProjectVersionPhysicalVolumeDto>> PhysicalVolumesByDirection,
    IReadOnlyDictionary<int, List<ProjectVersionPhysicalVolumeDto>> EquipmentVolumesByDirection);
