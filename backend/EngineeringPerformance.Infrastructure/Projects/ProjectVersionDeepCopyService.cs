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
    private readonly IProjectVersionPlanningParameterService _planningParameters;
    private readonly ILogger<ProjectVersionDeepCopyService> _logger;

    public ProjectVersionDeepCopyService(
        AppDbContext context,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IProjectVisibilityService visibility,
        IProjectVersionPlanningParameterService planningParameters,
        ILogger<ProjectVersionDeepCopyService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _visibility = visibility;
        _planningParameters = planningParameters;
        _logger = logger;
    }

    public async Task<ProjectVersionDto> CopyVersionAsync(
        int projectId,
        int versionId,
        string newVersionName,
        string? newDescription = null,
        AnalysisDirection? targetDirection = null,
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

        var resolvedDirection = VersionCopyDirectionRules.Resolve(source.Direction, targetDirection);

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
            "Deep copy started: project {ProjectId}, source version {SourceVersionId} ({SourceDirection}), target {TargetDirection}, new name {NewVersionName}, user {UserId}",
            projectId, versionId, source.Direction, resolvedDirection, newVersionName.Trim(), userId);

        // 3. Транзакция внутри execution strategy (EnableRetryOnFailure не поддерживает BeginTransactionAsync напрямую).
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var copy = await CopyVersionWithinTransactionAsync(
                    source,
                    newVersionName,
                    newDescription,
                    resolvedDirection,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Deep copy completed: project {ProjectId}, new version {NewVersionId} ({Direction}), PPV={PpvCount}, LI={LiCount}, equipment={EquipmentCount}",
                    projectId, copy.Id, copy.Direction, source.PhysicalVolumes.Count, source.LaborIntensities.Count, source.Equipments.Count);

                return MapToDto(copy);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task<ProjectAnalysisVersion> CopyVersionWithinTransactionAsync(
        ProjectAnalysisVersion source,
        string newVersionName,
        string? newDescription,
        AnalysisDirection targetDirection,
        CancellationToken cancellationToken)
    {
        var createdAt = DateTime.UtcNow;
        var sameDirection = targetDirection == source.Direction;

        var hasPeers = await _context.ProjectAnalysisVersions
            .AnyAsync(
                v => v.ProjectId == source.ProjectId
                     && v.Direction == targetDirection
                     && !v.IsArchived,
                cancellationToken);
        // Копия не наследует primary; становится основной только если это первая версия направления.
        var makePrimary = !hasPeers;

        var copy = new ProjectAnalysisVersion
        {
            ProjectId = source.ProjectId,
            Name = newVersionName.Trim(),
            VersionName = newVersionName.Trim(),
            Description = string.IsNullOrWhiteSpace(newDescription) ? source.Description : newDescription.Trim(),
            Direction = targetDirection,
            PhysicalVolumeTypeId = source.PhysicalVolumeTypeId,
            PlanningScenario = source.PlanningScenario,
            ZParameterLevel = source.ZParameterLevel,
            SortOrder = source.SortOrder,
            IsArchived = false,
            IsPrimary = makePrimary,
            CreatedAt = createdAt,
        };
        _context.ProjectAnalysisVersions.Add(copy);
        await _context.SaveChangesAsync(cancellationToken);

        var ppvIdMap = await CopyPhysicalVolumesAsync(
            source.PhysicalVolumes.ToList(),
            copy.Id,
            source.ProjectId,
            createdAt,
            cancellationToken);

        // PLI: только при Fact→Fact (у Plan обычно нет строк PLI; для Plan→Fact трудоёмкость импортируют отдельно).
        if (sameDirection && source.Direction == AnalysisDirection.Fact)
        {
            CopyLaborIntensities(source.LaborIntensities.ToList(), copy.Id, source.ProjectId, createdAt, ppvIdMap);
        }

        // Аудит импорта — только при копии в том же направлении.
        if (sameDirection)
        {
            CopyImportIterations(source.ImportIterations.ToList(), copy.Id, source.ProjectId);
        }

        CopyEquipments(source.Equipments.ToList(), copy.Id, source.ProjectId, createdAt);

        await AnalysisVersionSnapshotService.CopyFromVersionAsync(
            _context, source.Id, copy.Id, copy.CreatedAt, cancellationToken);

        await _planningParameters.CopyFromVersionAsync(source.Id, copy.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return copy;
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

        // Раньше SaveChanges вызывался на каждую строку (~12k) — копирование зависало.
        // Копируем уровнями BFS: один SaveChanges на уровень (для плоских данных — один раз).
        var byParent = sourcePpvs.ToLookup(pv => pv.ParentId);
        var currentLevel = byParent[null].ToList();

        while (currentLevel.Count > 0)
        {
            var copies = new List<ProjectPhysicalVolume>(currentLevel.Count);
            foreach (var src in currentLevel)
            {
                int? newParentId = src.ParentId is int parentId && ppvIdMap.TryGetValue(parentId, out var mappedParent)
                    ? mappedParent
                    : null;
                copies.Add(ClonePhysicalVolume(src, newVersionId, projectId, newParentId, createdAt));
            }

            _context.ProjectPhysicalVolumes.AddRange(copies);
            await _context.SaveChangesAsync(cancellationToken);

            for (var i = 0; i < currentLevel.Count; i++)
            {
                ppvIdMap[currentLevel[i].Id] = copies[i].Id;
            }

            var nextLevel = new List<ProjectPhysicalVolume>();
            foreach (var src in currentLevel)
            {
                nextLevel.AddRange(byParent[src.Id]);
            }

            currentLevel = nextLevel;
        }

        // Защита от циклов/осиротевших ParentId: оставшиеся узлы как корни.
        if (ppvIdMap.Count < sourcePpvs.Count)
        {
            var orphans = sourcePpvs.Where(pv => !ppvIdMap.ContainsKey(pv.Id)).ToList();
            var orphanCopies = orphans
                .Select(src => ClonePhysicalVolume(src, newVersionId, projectId, newParentId: null, createdAt))
                .ToList();
            _context.ProjectPhysicalVolumes.AddRange(orphanCopies);
            await _context.SaveChangesAsync(cancellationToken);
            for (var i = 0; i < orphans.Count; i++)
            {
                ppvIdMap[orphans[i].Id] = orphanCopies[i].Id;
            }
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
                LaborIntensityBase = eq.LaborIntensityBase,
                LaborIntensityPessimistic = eq.LaborIntensityPessimistic,
                LaborIntensityOptimistic = eq.LaborIntensityOptimistic,
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
            LaborIntensityBase = src.LaborIntensityBase,
            LaborIntensityPessimistic = src.LaborIntensityPessimistic,
            LaborIntensityOptimistic = src.LaborIntensityOptimistic,
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
            v.IsArchived,
            v.CreatedAt,
            v.PlanningScenario,
            v.ZParameterLevel,
            v.IsPrimary);
}
