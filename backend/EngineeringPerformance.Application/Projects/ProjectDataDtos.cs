namespace EngineeringPerformance.Application.Projects;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ProjectPhysicalVolumeDto(
    int Id,
    int ProjectId,
    int ProjectAnalysisVersionId,
    int PhysicalVolumeId,
    decimal Quantity,
    string? FacilityCode,
    string? FacilityName,
    string? WorkName,
    decimal? LaborHoursPerUnit,
    string? BindingType = null,
    string? DocumentSet = null,
    string? CatalogId = null,
    string? Tag = null,
    string? WorkId = null,
    string? WorkComment = null,
    string? EstimationBasis = null,
    string? CatalogCode = null,
    string? CatalogName = null);

public sealed record ProjectLaborIntensityDto(
    int Id,
    int ProjectId,
    int ProjectAnalysisVersionId,
    int? LaborIntensityId,
    decimal? PlannedHours,
    decimal? ActualHours,
    string? DocumentationCode = null,
    string? Brand = null,
    string? Notes = null,
    int? WorkDirectionId = null);

public sealed record ProjectEquipmentDto(
    int Id,
    int ProjectId,
    int ProjectAnalysisVersionId,
    int EquipmentId,
    decimal Quantity,
    decimal? QuantityInWork,
    string? PositionNumber = null,
    string? FacilityCode = null,
    string? FacilityName = null,
    string? DocumentSet = null,
    string? DiagramNumber = null,
    string? ShortName = null,
    string? CatalogName = null,
    string? TypeMark = null,
    string? CatalogId = null,
    string? Tag = null,
    string? FullEquipmentId = null,
    string? SupplyStandard = null,
    string? OlReference = null,
    string? EstimationBasis = null);

public sealed record ProjectPhysicalVolumeWriteRequest(
    int PhysicalVolumeId,
    decimal Quantity,
    string? FacilityCode = null,
    string? FacilityName = null,
    string? WorkName = null,
    decimal? LaborHoursPerUnit = null,
    string? BindingType = null,
    string? DocumentSet = null,
    string? CatalogId = null,
    string? Tag = null,
    string? WorkId = null,
    string? WorkComment = null,
    string? EstimationBasis = null);

public sealed record ProjectLaborIntensityWriteRequest(
    int? LaborIntensityId,
    decimal PlannedHours,
    decimal ActualHours,
    string? DocumentationCode = null,
    string? Brand = null,
    string? Notes = null);

public sealed record ProjectEquipmentWriteRequest(
    int EquipmentId,
    decimal Quantity,
    decimal? QuantityInWork = null,
    string? PositionNumber = null,
    string? FacilityCode = null,
    string? FacilityName = null,
    string? DocumentSet = null,
    string? DiagramNumber = null,
    string? ShortName = null,
    string? CatalogName = null,
    string? TypeMark = null,
    string? CatalogId = null,
    string? Tag = null,
    string? FullEquipmentId = null,
    string? SupplyStandard = null,
    string? OlReference = null,
    string? EstimationBasis = null);
