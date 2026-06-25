namespace EngineeringPerformance.Application.Projects;

public interface IProjectDataService
{
    Task<PagedResult<ProjectPhysicalVolumeDto>> GetPhysicalVolumesAsync(int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default);

    Task<PagedResult<ProjectLaborIntensityDto>> GetLaborIntensitiesAsync(int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default);

    Task<PagedResult<ProjectEquipmentDto>> GetEquipmentAsync(int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default);

    Task<ProjectPhysicalVolumeDto?> CreatePhysicalVolumeAsync(int projectId, int versionId, ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken = default);

    Task<ProjectPhysicalVolumeDto?> UpdatePhysicalVolumeAsync(int projectId, int versionId, int id, ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeletePhysicalVolumeAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default);

    Task<ProjectLaborIntensityDto?> CreateLaborIntensityAsync(int projectId, int versionId, ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken = default);

    Task<ProjectLaborIntensityDto?> UpdateLaborIntensityAsync(int projectId, int versionId, int id, ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteLaborIntensityAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default);

    Task<ProjectEquipmentDto?> CreateEquipmentAsync(int projectId, int versionId, ProjectEquipmentWriteRequest request, CancellationToken cancellationToken = default);

    Task<ProjectEquipmentDto?> UpdateEquipmentAsync(int projectId, int versionId, int id, ProjectEquipmentWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteEquipmentAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default);
}
