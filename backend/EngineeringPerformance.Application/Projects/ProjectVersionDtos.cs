using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

public sealed record ProjectVersionDto(
    int Id,
    int ProjectId,
    string Name,
    string VersionName,
    string? Description,
    AnalysisDirection Direction,
    int? PhysicalVolumeTypeId,
    int SortOrder,
    bool IsArchived);

public sealed record ProjectVersionWriteRequest(
    string VersionName,
    string? Description,
    AnalysisDirection Direction,
    int? PhysicalVolumeTypeId,
    string? Name = null);
