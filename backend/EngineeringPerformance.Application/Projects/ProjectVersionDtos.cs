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
    PlanningScenario PlanningScenario = PlanningScenario.ByEachValue,
    ZParameterLevel ZParameterLevel = ZParameterLevel.EachElement,
    bool IsPrimary = false,
    int RecalculatedVersionCount = 0,
    byte[]? RowVersion = null);

public sealed record ProjectVersionWriteRequest(
    string VersionName,
    string? Description,
    AnalysisDirection Direction,
    int? PhysicalVolumeTypeId,
    string? Name = null,
    PlanningScenario? PlanningScenario = null,
    ZParameterLevel? ZParameterLevel = null,
    /// <summary>
    /// Дата фиксации норм (снимок IIC). При создании задаёт <c>CreatedAt</c>;
    /// при обновлении — меняет <c>CreatedAt</c> и пересоздаёт снимок.
    /// </summary>
    DateTime? AsOfDate = null,
    /// <summary>Только Create: сделать версию основной (сбросит предыдущую того же Direction).</summary>
    bool? IsPrimary = null,
    byte[]? RowVersion = null);
