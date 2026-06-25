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
    decimal? LaborHoursPerUnit);

public sealed record ProjectLaborIntensityDto(
    int Id,
    int ProjectId,
    int ProjectAnalysisVersionId,
    int? LaborIntensityId,
    decimal? PlannedHours,
    decimal? ActualHours);

public sealed record ProjectEquipmentDto(
    int Id,
    int ProjectId,
    int ProjectAnalysisVersionId,
    int EquipmentId,
    decimal Quantity,
    decimal? QuantityInWork);

public sealed record ProjectPhysicalVolumeWriteRequest(
    int PhysicalVolumeId,
    decimal Quantity,
    string? FacilityCode,
    string? FacilityName,
    string? WorkName,
    decimal? LaborHoursPerUnit);

public sealed record ProjectLaborIntensityWriteRequest(
    int? LaborIntensityId,
    decimal PlannedHours,
    decimal ActualHours);

public sealed record ProjectEquipmentWriteRequest(
    int EquipmentId,
    decimal Quantity,
    decimal? QuantityInWork);
