using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;

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
    bool IsArchived,
    DateTime CreatedAt,
    PlanningScenario PlanningScenario = PlanningScenario.ByEachValue);

public sealed record ProjectVersionWriteRequest(
    string VersionName,
    string? Description,
    AnalysisDirection Direction,
    int? PhysicalVolumeTypeId,
    string? Name = null,
    PlanningScenario? PlanningScenario = null);
