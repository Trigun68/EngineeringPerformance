using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

public sealed record ProjectAnalysisVersionDto(
    int Id,
    int ProjectId,
    string ProjectName,
    string VersionName,
    AnalysisDirection Direction,
    string? PhysicalVolumeTypeName,
    DateTime CreatedAt);
