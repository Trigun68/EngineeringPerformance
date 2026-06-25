using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EngineeringPerformance.Infrastructure.Projects;

/// <summary>
/// Deep Copy версии проекта: PPV (иерархия), LI, итерации импорта, Equipment, новый снимок норм.
/// </summary>
public sealed class ProjectVersionDeepCopyService : IProjectVersionDeepCopyService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly IProjectVisibilityService _visibility;
    private readonly ILogger<ProjectVersionDeepCopyService> _logger;

    public ProjectVersionDeepCopyService(
        AppDbContext context,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IProjectVisibilityService visibility,
        ILogger<ProjectVersionDeepCopyService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _visibility = visibility;
        _logger = logger;
    }

    public async Task<ProjectVersionDto> CopyVersionAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newVersionName))
            throw new ArgumentException("New version name is required.", nameof(newVersionName));

        // 1. Загрузка исходной версии со всеми связями (один запрос, AsNoTracking).
        var source = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.PhysicalVolumes)
            .Include(v => v.LaborIntensities)
            .Include(v => v.ImportIterations)
            .Include(v => v.Equipments)
            .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Version {versionId} not found for project {projectId}.");

        // 2. Проверка прав (defense-in-depth к атрибутам контроллера).
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        if (!await _permissionService.CanEditAsync(userId, PermissionType.ProjectAnalysisVersions, cancellationToken))
            throw new UnauthorizedAccessException("User lacks Edit permission for project versions.");
        if (!await _permissionService.CanEditAsync(userId, PermissionType.ProjectDataUpload, cancellationToken))
            throw new UnauthorizedAccessException("User cannot upload/copy project data.");
        if (!await _visibility.CanAccessProjectAsync(userId, projectId, AccessLevel.Edit, cancellationToken))
            throw new UnauthorizedAccessException($"User {userId} cannot edit project {projectId}.");

        _logger.LogInformation(
            "Deep copy started: project {ProjectId}, source version {SourceVersionId}, new name {NewVersionName}, user {UserId}",
            projectId, versionId, newVersionName.Trim(), userId);

        // 3. Транзакция: всё или ничего.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var createdAt = DateTime.UtcNow;
            var copy = new ProjectAnalysisVersion
            {
                ProjectId = source.ProjectId,
                Name = newVersionName.Trim(),
                VersionName = newVersionName.Trim(),
                Description = string.IsNullOrWhiteSpace(newDescription) ? source.Description : newDescription.Trim(),
                Direction = source.Direction,
                PhysicalVolumeTypeId = source.PhysicalVolumeTypeId,
                SortOrder = source.SortOrder,
                IsArchived = false,
                CreatedAt = createdAt,
            };
            _context.ProjectAnalysisVersions.Add(copy);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Deep Copy PPV с сохранением иерархии через ppvIdMap.
            var ppvIdMap = await CopyPhysicalVolumesAsync(
                source.PhysicalVolumes.ToList(),
                copy.Id,
                source.ProjectId,
                createdAt,
                cancellationToken);

            // 5. Deep Copy LI (Fact): ремап ProjectPhysicalVolumeId на новые PPV.
            if (source.Direction == AnalysisDirection.Fact)
            {
                CopyLaborIntensities(source.LaborIntensities.ToList(), copy.Id, source.ProjectId, createdAt, ppvIdMap);
            }

            // 6. Копирование итераций импорта (аудит 1-в-1).
            CopyImportIterations(source.ImportIterations.ToList(), copy.Id, source.ProjectId);

            // 7. GAP-fix WPF: копирование ProjectEquipments (раньше пропускалось).
            CopyEquipments(source.Equipments.ToList(), copy.Id, source.ProjectId, createdAt);

            // 8. Новый снимок норм на copy.CreatedAt (не клон старого).
            await AnalysisVersionSnapshotService.PopulateSnapshotAsync(
                _context, copy.Id, createdAt, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Deep copy completed: project {ProjectId}, new version {NewVersionId}, PPV={PpvCount}, LI={LiCount}, equipment={EquipmentCount}",
                projectId, copy.Id, source.PhysicalVolumes.Count, source.LaborIntensities.Count, source.Equipments.Count);

            return MapToDto(copy);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Dictionary<int, int>> CopyPhysicalVolumesAsync(
        IReadOnlyCollection<ProjectPhysicalVolume> sourcePpvs,
        int newVersionId,
        int projectId,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var ppvIdMap = new Dictionary<int, int>();
        if (sourcePpvs.Count == 0)
            return ppvIdMap;

        var byParent = sourcePpvs.ToLookup(pv => pv.ParentId);
        var queue = new Queue<ProjectPhysicalVolume>(byParent[null]);

        while (queue.Count > 0)
        {
            var src = queue.Dequeue();
            int? newParentId = src.ParentId is int parentId && ppvIdMap.TryGetValue(parentId, out var mappedParent)
                ? mappedParent
                : null;

            var copy = ClonePhysicalVolume(src, newVersionId, projectId, newParentId, createdAt);
            _context.ProjectPhysicalVolumes.Add(copy);
            await _context.SaveChangesAsync(cancellationToken);

            ppvIdMap[src.Id] = copy.Id;

            foreach (var child in byParent[src.Id])
                queue.Enqueue(child);
        }

        return ppvIdMap;
    }

    private void CopyLaborIntensities(
        IReadOnlyCollection<ProjectLaborIntensity> sourceLis,
        int newVersionId,
        int projectId,
        DateTime createdAt,
        IReadOnlyDictionary<int, int> ppvIdMap)
    {
        foreach (var li in sourceLis)
        {
            int? newProjectPpvId = null;
            if (li.ProjectPhysicalVolumeId is int oldPpvId)
            {
                if (!ppvIdMap.TryGetValue(oldPpvId, out var mapped))
                    throw new InvalidOperationException($"LI references unknown PPV {oldPpvId}");
                newProjectPpvId = mapped;
            }

            _context.ProjectLaborIntensities.Add(new ProjectLaborIntensity
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = newVersionId,
                DocumentationCode = li.DocumentationCode,
                WorkDirectionId = li.WorkDirectionId,
                LaborIntensityId = li.LaborIntensityId,
                PhysicalVolumeKfoId = li.PhysicalVolumeKfoId,
                PhysicalVolumeBerId = li.PhysicalVolumeBerId,
                ProjectPhysicalVolumeId = newProjectPpvId,
                PlannedHours = li.PlannedHours,
                ActualHours = li.ActualHours,
                Notes = li.Notes,
                Brand = li.Brand,
                CreatedAt = createdAt,
            });
        }
    }

    private void CopyImportIterations(
        IReadOnlyCollection<DataImportIteration> sourceIterations,
        int newVersionId,
        int projectId)
    {
        foreach (var it in sourceIterations)
        {
            _context.DataImportIterations.Add(new DataImportIteration
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = newVersionId,
                DataType = it.DataType,
                ImportedAt = it.ImportedAt,
                Description = it.Description,
                RecordCount = it.RecordCount,
                ImportedByUserId = it.ImportedByUserId,
                SourceFileName = it.SourceFileName,
                SourceFileHash = it.SourceFileHash,
            });
        }
    }

    private void CopyEquipments(
        IReadOnlyCollection<ProjectEquipment> sourceEquipment,
        int newVersionId,
        int projectId,
        DateTime createdAt)
    {
        foreach (var eq in sourceEquipment)
        {
            _context.ProjectEquipments.Add(new ProjectEquipment
            {
                ProjectId = projectId,
                ProjectAnalysisVersionId = newVersionId,
                EquipmentId = eq.EquipmentId,
                Quantity = eq.Quantity,
                QuantityInWork = eq.QuantityInWork,
                PositionNumber = eq.PositionNumber,
                FacilityCode = eq.FacilityCode,
                FacilityName = eq.FacilityName,
                DocumentSet = eq.DocumentSet,
                DiagramNumber = eq.DiagramNumber,
                ShortName = eq.ShortName,
                CatalogName = eq.CatalogName,
                TypeMark = eq.TypeMark,
                CatalogId = eq.CatalogId,
                Tag = eq.Tag,
                FullEquipmentId = eq.FullEquipmentId,
                SupplyStandard = eq.SupplyStandard,
                OlReference = eq.OlReference,
                EstimationBasis = eq.EstimationBasis,
                CreatedAt = createdAt,
            });
        }
    }

    private static ProjectPhysicalVolume ClonePhysicalVolume(
        ProjectPhysicalVolume src,
        int newVersionId,
        int projectId,
        int? newParentId,
        DateTime createdAt) =>
        new()
        {
            ProjectId = projectId,
            ProjectAnalysisVersionId = newVersionId,
            ParentId = newParentId,
            PhysicalVolumeKfoId = src.PhysicalVolumeKfoId,
            PhysicalVolumeBerId = src.PhysicalVolumeBerId,
            Quantity = src.Quantity,
            FacilityCode = src.FacilityCode,
            FacilityName = src.FacilityName,
            BindingType = src.BindingType,
            DocumentSet = src.DocumentSet,
            CatalogId = src.CatalogId,
            Tag = src.Tag,
            WorkId = src.WorkId,
            WorkName = src.WorkName,
            WorkComment = src.WorkComment,
            EstimationBasis = src.EstimationBasis,
            CatalogCode = src.CatalogCode,
            CatalogName = src.CatalogName,
            LaborHoursPerUnit = src.LaborHoursPerUnit,
            MachineHoursPerUnit = src.MachineHoursPerUnit,
            TotalMachineHours = src.TotalMachineHours,
            Status = src.Status,
            CreatedAt = createdAt,
        };

    private static ProjectVersionDto MapToDto(ProjectAnalysisVersion v) =>
        new(
            v.Id,
            v.ProjectId,
            v.Name,
            v.VersionName,
            v.Description,
            v.Direction,
            v.PhysicalVolumeTypeId,
            v.SortOrder,
            v.IsArchived);
}
