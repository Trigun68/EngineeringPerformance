using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectDataService : IProjectDataService
{
    private readonly AppDbContext _context;

    public ProjectDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProjectPhysicalVolumeDto>> GetPhysicalVolumesAsync(
        int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectPhysicalVolumes.AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId);

        var total = await query.CountAsync(cancellationToken);
        var items = await ProjectDataSort.Apply(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProjectPhysicalVolumeDto(
                x.Id, x.ProjectId, x.ProjectAnalysisVersionId,
                x.PhysicalVolumeKfoId ?? x.PhysicalVolumeBerId ?? 0,
                x.Quantity, x.FacilityCode, x.FacilityName, x.WorkName, x.LaborHoursPerUnit))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectPhysicalVolumeDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<ProjectLaborIntensityDto>> GetLaborIntensitiesAsync(
        int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectLaborIntensities.AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId);

        var total = await query.CountAsync(cancellationToken);
        var items = await ProjectDataSort.Apply(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProjectLaborIntensityDto(
                x.Id, x.ProjectId, x.ProjectAnalysisVersionId, x.LaborIntensityId,
                x.PlannedHours, x.ActualHours))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectLaborIntensityDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<ProjectEquipmentDto>> GetEquipmentAsync(
        int projectId, int versionId, int page, int pageSize, string? sort = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectEquipments.AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId);

        var total = await query.CountAsync(cancellationToken);
        var items = await ProjectDataSort.Apply(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProjectEquipmentDto(
                x.Id, x.ProjectId, x.ProjectAnalysisVersionId, x.EquipmentId,
                x.Quantity, x.QuantityInWork))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProjectEquipmentDto>(items, total, page, pageSize);
    }

    public async Task<ProjectPhysicalVolumeDto?> CreatePhysicalVolumeAsync(
        int projectId, int versionId, ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (!await VersionBelongsToProjectAsync(projectId, versionId, cancellationToken))
            return null;

        if (!await PhysicalVolumeCatalogBinding.CatalogEntryExistsAsync(_context, versionId, request.PhysicalVolumeId, cancellationToken))
            throw new InvalidOperationException($"Catalog volume {request.PhysicalVolumeId} not found for version {versionId}.");

        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(_context, versionId, cancellationToken);
        var entity = new ProjectPhysicalVolume
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            Quantity = request.Quantity,
            FacilityCode = request.FacilityCode,
            FacilityName = request.FacilityName,
            WorkName = request.WorkName,
            LaborHoursPerUnit = request.LaborHoursPerUnit,
        };
        PhysicalVolumeCatalogBinding.AssignCatalogId(entity, request.PhysicalVolumeId, isKfo);

        _context.ProjectPhysicalVolumes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return MapPpv(entity);
    }

    public async Task<ProjectPhysicalVolumeDto?> UpdatePhysicalVolumeAsync(
        int projectId, int versionId, int id, ProjectPhysicalVolumeWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await FindPpvAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return null;

        if (!await PhysicalVolumeCatalogBinding.CatalogEntryExistsAsync(_context, versionId, request.PhysicalVolumeId, cancellationToken))
            throw new InvalidOperationException($"Catalog volume {request.PhysicalVolumeId} not found for version {versionId}.");

        var isKfo = await PhysicalVolumeCatalogBinding.IsKfoVersionAsync(_context, versionId, cancellationToken);
        PhysicalVolumeCatalogBinding.AssignCatalogId(entity, request.PhysicalVolumeId, isKfo);
        entity.Quantity = request.Quantity;
        entity.FacilityCode = request.FacilityCode;
        entity.FacilityName = request.FacilityName;
        entity.WorkName = request.WorkName;
        entity.LaborHoursPerUnit = request.LaborHoursPerUnit;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapPpv(entity);
    }

    public async Task<bool> DeletePhysicalVolumeAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default)
    {
        var entity = await FindPpvAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return false;

        _context.ProjectPhysicalVolumes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProjectLaborIntensityDto?> CreateLaborIntensityAsync(
        int projectId, int versionId, ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (!await VersionBelongsToProjectAsync(projectId, versionId, cancellationToken))
            return null;

        if (request.LaborIntensityId.HasValue &&
            !await _context.LaborIntensities.AnyAsync(x => x.Id == request.LaborIntensityId.Value, cancellationToken))
            throw new InvalidOperationException($"LaborIntensity {request.LaborIntensityId} not found.");

        var entity = new ProjectLaborIntensity
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            LaborIntensityId = request.LaborIntensityId,
            PlannedHours = request.PlannedHours,
            ActualHours = request.ActualHours,
        };

        _context.ProjectLaborIntensities.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return MapLi(entity);
    }

    public async Task<ProjectLaborIntensityDto?> UpdateLaborIntensityAsync(
        int projectId, int versionId, int id, ProjectLaborIntensityWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await FindLiAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return null;

        if (request.LaborIntensityId.HasValue &&
            !await _context.LaborIntensities.AnyAsync(x => x.Id == request.LaborIntensityId.Value, cancellationToken))
            throw new InvalidOperationException($"LaborIntensity {request.LaborIntensityId} not found.");

        entity.LaborIntensityId = request.LaborIntensityId;
        entity.PlannedHours = request.PlannedHours;
        entity.ActualHours = request.ActualHours;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapLi(entity);
    }

    public async Task<bool> DeleteLaborIntensityAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default)
    {
        var entity = await FindLiAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return false;

        _context.ProjectLaborIntensities.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProjectEquipmentDto?> CreateEquipmentAsync(
        int projectId, int versionId, ProjectEquipmentWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (!await VersionBelongsToProjectAsync(projectId, versionId, cancellationToken))
            return null;

        if (!await _context.Equipments.AnyAsync(x => x.Id == request.EquipmentId, cancellationToken))
            throw new InvalidOperationException($"Equipment {request.EquipmentId} not found.");

        var entity = new ProjectEquipment
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = versionId,
            EquipmentId = request.EquipmentId,
            Quantity = request.Quantity,
            QuantityInWork = request.QuantityInWork,
        };

        _context.ProjectEquipments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return MapEq(entity);
    }

    public async Task<ProjectEquipmentDto?> UpdateEquipmentAsync(
        int projectId, int versionId, int id, ProjectEquipmentWriteRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await FindEqAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return null;

        if (!await _context.Equipments.AnyAsync(x => x.Id == request.EquipmentId, cancellationToken))
            throw new InvalidOperationException($"Equipment {request.EquipmentId} not found.");

        entity.EquipmentId = request.EquipmentId;
        entity.Quantity = request.Quantity;
        entity.QuantityInWork = request.QuantityInWork;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapEq(entity);
    }

    public async Task<bool> DeleteEquipmentAsync(int projectId, int versionId, int id, CancellationToken cancellationToken = default)
    {
        var entity = await FindEqAsync(projectId, versionId, id, cancellationToken);
        if (entity is null)
            return false;

        _context.ProjectEquipments.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<bool> VersionBelongsToProjectAsync(int projectId, int versionId, CancellationToken cancellationToken) =>
        _context.ProjectAnalysisVersions.AnyAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken);

    private Task<ProjectPhysicalVolume?> FindPpvAsync(int projectId, int versionId, int id, CancellationToken cancellationToken) =>
        _context.ProjectPhysicalVolumes.FirstOrDefaultAsync(
            x => x.Id == id && x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId,
            cancellationToken);

    private Task<ProjectLaborIntensity?> FindLiAsync(int projectId, int versionId, int id, CancellationToken cancellationToken) =>
        _context.ProjectLaborIntensities.FirstOrDefaultAsync(
            x => x.Id == id && x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId,
            cancellationToken);

    private Task<ProjectEquipment?> FindEqAsync(int projectId, int versionId, int id, CancellationToken cancellationToken) =>
        _context.ProjectEquipments.FirstOrDefaultAsync(
            x => x.Id == id && x.ProjectId == projectId && x.ProjectAnalysisVersionId == versionId,
            cancellationToken);

    private static ProjectPhysicalVolumeDto MapPpv(ProjectPhysicalVolume x) =>
        new(x.Id, x.ProjectId, x.ProjectAnalysisVersionId, x.GetCatalogVolumeId() ?? 0,
            x.Quantity, x.FacilityCode, x.FacilityName, x.WorkName, x.LaborHoursPerUnit);

    private static ProjectLaborIntensityDto MapLi(ProjectLaborIntensity x) =>
        new(x.Id, x.ProjectId, x.ProjectAnalysisVersionId, x.LaborIntensityId, x.PlannedHours, x.ActualHours);

    private static ProjectEquipmentDto MapEq(ProjectEquipment x) =>
        new(x.Id, x.ProjectId, x.ProjectAnalysisVersionId, x.EquipmentId, x.Quantity, x.QuantityInWork);
}
