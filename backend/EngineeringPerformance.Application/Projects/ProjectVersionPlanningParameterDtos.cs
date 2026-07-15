namespace EngineeringPerformance.Application.Projects;

public sealed record ProjectVersionPlanningParameterDto(
    int Id,
    int IicWorkDirectionId,
    int? ParentId,
    int SortOrder,
    string Code,
    string Name,
    string? UnitName,
    decimal ZPessimistic,
    decimal ZBase,
    decimal ZOptimistic,
    decimal? ZActual);

public sealed record SaveProjectVersionPlanningParametersRequest(
    IReadOnlyList<ProjectVersionPlanningParameterWriteItem> Items);

public sealed record ProjectVersionPlanningParameterWriteItem(
    int IicWorkDirectionId,
    decimal ZPessimistic,
    decimal ZBase,
    decimal ZOptimistic);
