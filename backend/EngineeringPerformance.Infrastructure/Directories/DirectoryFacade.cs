using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Directories;

public sealed class DirectoryFacade : IDirectoryFacade
{
    private readonly AppDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator;

    private static readonly HashSet<string> AssessmentCatalogResources = new(StringComparer.OrdinalIgnoreCase)
    {
        "physical-volumes-kfo",
        "physical-volumes-ber",
        "equipment",
    };

    public DirectoryFacade(
        AppDbContext context,
        ICacheInvalidator cacheInvalidator,
        IAnalysisVersionRecalculationOrchestrator recalcOrchestrator)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
        _recalcOrchestrator = recalcOrchestrator;
    }

    public async Task<IReadOnlyList<DirectoryEntryDto>> ListAsync(
        string resource,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null)
    {
        return await DispatchList(resource, includeArchived, cancellationToken, projectId);
    }

    public async Task<IReadOnlyList<DirectoryEntryDto>> GetChildrenAsync(
        string resource,
        int? parentId,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null)
    {
        return await DispatchChildren(resource, parentId, includeArchived, cancellationToken, projectId);
    }

    public Task<IReadOnlyList<DirectoryTreeNodeDto>> GetTreeAsync(
        string resource,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null) =>
        resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => MapTree<PhysicalVolumeKfo>(includeArchived, cancellationToken),
            "physical-volumes-ber" => MapTree<PhysicalVolumeBer>(includeArchived, cancellationToken),
            "equipment" => MapEquipmentTree(includeArchived, projectId, cancellationToken),
            "labor-intensities" => MapTree<LaborIntensity>(includeArchived, cancellationToken),
            "iic-work-directions" => MapTree<IicWorkDirection>(includeArchived, cancellationToken),
            _ => throw new InvalidOperationException($"Resource '{resource}' does not support tree view.")
        };

    public async Task<DirectoryEntryDto?> GetByIdAsync(string resource, int id, CancellationToken cancellationToken)
    {
        var entity = await DispatchGetById(resource, id, cancellationToken);
        return entity is null ? null : DirectoryEntityMapper.ToDto(entity);
    }

    public async Task<DirectoryEntryDto> CreateAsync(string resource, DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(resource, request, entityId: null, cancellationToken);
        var created = await DispatchCreate(resource, request, cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        // Reload без навигаций — единообразно с UpdateAsync (защита от циклов ToDto).
        var forDto = await DispatchGetById(resource, created.Id, cancellationToken) ?? created;
        return DirectoryEntityMapper.ToDto(forDto);
    }

    public async Task<DirectoryEntryDto?> UpdateAsync(string resource, int id, DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(resource, request, entityId: id, cancellationToken);

        var before = AssessmentCatalogResources.Contains(resource)
            ? await ReadAssessmentCoefficientAsync(resource, id, cancellationToken)
            : null;

        var updated = await DispatchUpdate(resource, id, request, cancellationToken);
        if (updated is null)
            return null;

        await _cacheInvalidator.InvalidateAsync(cancellationToken);

        var recalcCount = 0;
        if (before is not null)
        {
            var after = ReadAssessmentCoefficient(updated);
            if (after is not null
                && (before.Value.IsAssessment != after.Value.IsAssessment
                    || before.Value.Coefficient != after.Value.Coefficient))
            {
                recalcCount = await _recalcOrchestrator.AfterCatalogAssessmentOrCoefficientChangedAsync(
                    resource, id, cancellationToken);
            }
        }

        // ToDto после recalc: reload без навигаций (EF fixup мог наполнить ProjectPhysicalVolumes).
        var forDto = await DispatchGetById(resource, id, cancellationToken) ?? updated;
        return DirectoryEntityMapper.ToDto(forDto) with { RecalculatedVersionCount = recalcCount };
    }

    private async Task<(bool? IsAssessment, decimal Coefficient)?> ReadAssessmentCoefficientAsync(
        string resource,
        int id,
        CancellationToken cancellationToken)
    {
        switch (resource.ToLowerInvariant())
        {
            case "physical-volumes-kfo":
            {
                var row = await _context.PhysicalVolumesKfo.AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new { x.IsAssessment, x.Coefficient })
                    .FirstOrDefaultAsync(cancellationToken);
                return row is null ? null : (row.IsAssessment, row.Coefficient);
            }
            case "physical-volumes-ber":
            {
                var row = await _context.PhysicalVolumesBer.AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new { x.IsAssessment, x.Coefficient })
                    .FirstOrDefaultAsync(cancellationToken);
                return row is null ? null : (row.IsAssessment, row.Coefficient);
            }
            case "equipment":
            {
                var row = await _context.Equipments.AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new { x.IsAssessment, x.Coefficient })
                    .FirstOrDefaultAsync(cancellationToken);
                return row is null ? null : (row.IsAssessment, row.Coefficient);
            }
            default:
                return null;
        }
    }

    private static (bool? IsAssessment, decimal Coefficient)? ReadAssessmentCoefficient(DirectoryItem item) =>
        item switch
        {
            PhysicalVolumeKfo k => (k.IsAssessment, k.Coefficient),
            PhysicalVolumeBer b => (b.IsAssessment, b.Coefficient),
            Equipment e => (e.IsAssessment, e.Coefficient),
            _ => null,
        };

    public async Task<bool> ArchiveAsync(string resource, int id, CancellationToken cancellationToken)
    {
        var result = await DispatchArchive(resource, id, cancellationToken);
        if (result)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return result;
    }

    public async Task<bool> UnarchiveAsync(string resource, int id, CancellationToken cancellationToken)
    {
        var result = await DispatchUnarchive(resource, id, cancellationToken);
        if (result)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return result;
    }

    public async Task<bool> DeleteAsync(string resource, int id, CancellationToken cancellationToken)
    {
        await EnsureNotInUseAsync(resource, id, cancellationToken);
        var result = await DispatchDelete(resource, id, cancellationToken);
        if (result)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return result;
    }

    /// <summary>IMP-01: не удалять справочник, если на него ссылаются проектные/дочерние записи.</summary>
    private async Task EnsureNotInUseAsync(string resource, int id, CancellationToken cancellationToken)
    {
        var inUse = resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" =>
                await _context.ProjectPhysicalVolumes.AsNoTracking().AnyAsync(x => x.PhysicalVolumeKfoId == id, cancellationToken)
                || await _context.PhysicalVolumesKfo.AsNoTracking().AnyAsync(x => x.ParentId == id, cancellationToken),
            "physical-volumes-ber" =>
                await _context.ProjectPhysicalVolumes.AsNoTracking().AnyAsync(x => x.PhysicalVolumeBerId == id, cancellationToken)
                || await _context.PhysicalVolumesBer.AsNoTracking().AnyAsync(x => x.ParentId == id, cancellationToken),
            "equipment" =>
                await _context.ProjectEquipments.AsNoTracking().AnyAsync(x => x.EquipmentId == id, cancellationToken)
                || await _context.Equipments.AsNoTracking().AnyAsync(x => x.ParentId == id, cancellationToken),
            "labor-intensities" =>
                await _context.ProjectLaborIntensities.AsNoTracking().AnyAsync(x => x.LaborIntensityId == id, cancellationToken)
                || await _context.LaborIntensities.AsNoTracking().AnyAsync(x => x.ParentId == id, cancellationToken),
            "iic-work-directions" =>
                await _context.IicWorkDirections.AsNoTracking().AnyAsync(x => x.ParentId == id, cancellationToken)
                || await _context.IicWorkDirectionNorms.AsNoTracking().AnyAsync(x => x.WorkDirectionId == id, cancellationToken)
                || await _context.IicWorkDirectionWorkCosts.AsNoTracking().AnyAsync(x => x.WorkDirectionId == id, cancellationToken)
                || await _context.PhysicalVolumesKfo.AsNoTracking().AnyAsync(x => x.ClassifierId == id, cancellationToken)
                || await _context.PhysicalVolumesBer.AsNoTracking().AnyAsync(x => x.ClassifierId == id, cancellationToken)
                || await _context.Equipments.AsNoTracking().AnyAsync(x => x.ClassifierId == id, cancellationToken)
                || await _context.LaborIntensities.AsNoTracking().AnyAsync(x => x.IicWorkDirectionId == id, cancellationToken),
            "units-of-measurement" =>
                await _context.PhysicalVolumesKfo.AsNoTracking().AnyAsync(x => x.UnitId == id, cancellationToken)
                || await _context.PhysicalVolumesBer.AsNoTracking().AnyAsync(x => x.UnitId == id, cancellationToken)
                || await _context.Equipments.AsNoTracking().AnyAsync(x => x.UnitId == id, cancellationToken)
                || await _context.IicWorkDirections.AsNoTracking().AnyAsync(x => x.UnitId == id, cancellationToken),
            "doc-sets" =>
                await _context.ProjectLaborIntensities.AsNoTracking().AnyAsync(x => x.DocSetId == id, cancellationToken),
            _ => false,
        };

        if (inUse)
            throw new InvalidOperationException(
                $"Cannot delete '{resource}' id={id}: the entry is referenced by other records. Archive it instead.");
    }

    public async Task<bool> UpdateSortOrderAsync(string resource, int id, SortOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await DispatchSortOrder(resource, id, request, cancellationToken);
        if (result)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<CharacteristicOptionDto>> GetCharacteristicOptionsAsync(int characteristicId, CancellationToken cancellationToken)
    {
        await EnsureCharacteristicExists(characteristicId, cancellationToken);
        return await _context.ProjectCharacteristicOptions.AsNoTracking()
            .Where(x => x.CharacteristicId == characteristicId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CharacteristicOptionDto(x.Id, x.CharacteristicId, x.Name, x.ValuePercent, x.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<CharacteristicOptionDto> CreateCharacteristicOptionAsync(int characteristicId, CharacteristicOptionWriteRequest request, CancellationToken cancellationToken)
    {
        await EnsureCharacteristicExists(characteristicId, cancellationToken);
        var option = new ProjectCharacteristicOption
        {
            CharacteristicId = characteristicId,
            Name = request.Name.Trim(),
            ValuePercent = request.ValuePercent,
            SortOrder = request.SortOrder,
        };
        _context.ProjectCharacteristicOptions.Add(option);
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return new CharacteristicOptionDto(option.Id, option.CharacteristicId, option.Name, option.ValuePercent, option.SortOrder);
    }

    public async Task<CharacteristicOptionDto?> UpdateCharacteristicOptionAsync(int characteristicId, int optionId, CharacteristicOptionWriteRequest request, CancellationToken cancellationToken)
    {
        var option = await _context.ProjectCharacteristicOptions
            .FirstOrDefaultAsync(x => x.Id == optionId && x.CharacteristicId == characteristicId, cancellationToken);
        if (option is null)
            return null;

        option.Name = request.Name.Trim();
        option.ValuePercent = request.ValuePercent;
        option.SortOrder = request.SortOrder;
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return new CharacteristicOptionDto(option.Id, option.CharacteristicId, option.Name, option.ValuePercent, option.SortOrder);
    }

    public async Task<bool> DeleteCharacteristicOptionAsync(int characteristicId, int optionId, CancellationToken cancellationToken)
    {
        var option = await _context.ProjectCharacteristicOptions
            .FirstOrDefaultAsync(x => x.Id == optionId && x.CharacteristicId == characteristicId, cancellationToken);
        if (option is null)
            return false;

        _context.ProjectCharacteristicOptions.Remove(option);
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return true;
    }

    private async Task EnsureCharacteristicExists(int characteristicId, CancellationToken cancellationToken)
    {
        var exists = await _context.ProjectCharacteristics.AsNoTracking()
            .AnyAsync(x => x.Id == characteristicId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"ProjectCharacteristic {characteristicId} not found.");
    }

    private async Task ValidateReferencesAsync(
        string resource,
        DirectoryWriteRequest request,
        int? entityId,
        CancellationToken cancellationToken)
    {
        var validator = new DirectoryReferenceValidator(_context);
        await validator.ValidateAsync(resource, request, entityId, cancellationToken);
    }

    private async Task<IReadOnlyList<DirectoryTreeNodeDto>> MapTree<T>(bool includeArchived, CancellationToken cancellationToken)
        where T : DirectoryItem, new()
    {
        var items = await Repo<T>().GetAllAsync(includeArchived, cancellationToken);
        return DirectoryEntityMapper.BuildTree(items);
    }

    private async Task<IReadOnlyList<DirectoryTreeNodeDto>> MapEquipmentTree(
        bool includeArchived,
        int? projectId,
        CancellationToken cancellationToken)
    {
        var items = FilterEquipment(
            await Repo<Equipment>().GetAllAsync(includeArchived, cancellationToken),
            projectId);
        return DirectoryEntityMapper.BuildTree(items);
    }

    private static IReadOnlyList<Equipment> FilterEquipment(
        IReadOnlyList<Equipment> items,
        int? projectId)
    {
        // Без projectId — все проектные строки (скрываем legacy NULL).
        // С projectId — только выбранный проект.
        IEnumerable<Equipment> query = items.Where(e => e.ProjectId != null);
        if (projectId is int pid)
            query = query.Where(e => e.ProjectId == pid);
        return query.ToList();
    }

    private EfDirectoryRepository<T> Repo<T>() where T : DirectoryItem, new() =>
        new(_context, GetDbSet<T>());

    private static Func<AppDbContext, DbSet<T>> GetDbSet<T>() where T : DirectoryItem =>
        typeof(T).Name switch
        {
            nameof(PhysicalVolumeType) => ctx => (DbSet<T>)(object)ctx.PhysicalVolumeTypes,
            nameof(PhysicalVolumeKfo) => ctx => (DbSet<T>)(object)ctx.PhysicalVolumesKfo,
            nameof(PhysicalVolumeBer) => ctx => (DbSet<T>)(object)ctx.PhysicalVolumesBer,
            nameof(Equipment) => ctx => (DbSet<T>)(object)ctx.Equipments,
            nameof(LaborIntensity) => ctx => (DbSet<T>)(object)ctx.LaborIntensities,
            nameof(IicWorkDirection) => ctx => (DbSet<T>)(object)ctx.IicWorkDirections,
            nameof(UnitOfMeasurement) => ctx => (DbSet<T>)(object)ctx.UnitsOfMeasurement,
            nameof(DocSet) => ctx => (DbSet<T>)(object)ctx.DocSets,
            nameof(ProjectCharacteristic) => ctx => (DbSet<T>)(object)ctx.ProjectCharacteristics,
            _ => throw new InvalidOperationException($"Unsupported directory entity {typeof(T).Name}")
        };

    private static void EnsureResource(string resource)
    {
        if (!DirectoryResourceCatalog.TryGet(resource, out _))
            throw new KeyNotFoundException($"Unknown directory resource '{resource}'.");
    }

    private static T Prepare<T>(T entity, DirectoryWriteRequest request) where T : DirectoryItem, new()
    {
        var merged = DirectoryEntityMapper.Merge(request, entity);
        if (merged is LaborIntensity labor && string.IsNullOrWhiteSpace(labor.DocumentTypeName))
        {
            var name = (labor.Name ?? string.Empty).Trim();
            if (name.Length > 0)
                labor.DocumentTypeName = name;
        }

        if (merged is Equipment equipment)
        {
            if (equipment.ProjectId is null
                && request.Properties is not null
                && request.Properties.TryGetValue("projectId", out var projectEl)
                && projectEl.ValueKind == System.Text.Json.JsonValueKind.Number
                && projectEl.TryGetInt32(out var projectIdFromRequest))
            {
                equipment.ProjectId = projectIdFromRequest;
            }

            equipment.CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(
                equipment.ProjectId,
                equipment.Name,
                equipment.UnitId);
        }

        return merged;
    }

    private async Task<IReadOnlyList<DirectoryEntryDto>> DispatchList(
        string resource,
        bool includeArchived,
        CancellationToken ct,
        int? projectId)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => (await Repo<PhysicalVolumeKfo>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "physical-volumes-ber" => (await Repo<PhysicalVolumeBer>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "equipment" => FilterEquipment(await Repo<Equipment>().GetAllAsync(includeArchived, ct), projectId)
                .Select(DirectoryEntityMapper.ToDto).ToList(),
            "labor-intensities" => (await Repo<LaborIntensity>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "iic-work-directions" => (await Repo<IicWorkDirection>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "units-of-measurement" => (await Repo<UnitOfMeasurement>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "doc-sets" => (await Repo<DocSet>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "project-characteristics" => (await Repo<ProjectCharacteristic>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<IReadOnlyList<DirectoryEntryDto>> DispatchChildren(
        string resource,
        int? parentId,
        bool includeArchived,
        CancellationToken ct,
        int? projectId)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => (await Repo<PhysicalVolumeKfo>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "physical-volumes-ber" => (await Repo<PhysicalVolumeBer>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "equipment" => FilterEquipment(
                    await Repo<Equipment>().GetChildrenAsync(parentId, includeArchived, ct),
                    projectId)
                .Select(DirectoryEntityMapper.ToDto).ToList(),
            "labor-intensities" => (await Repo<LaborIntensity>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "iic-work-directions" => (await Repo<IicWorkDirection>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "units-of-measurement" => (await Repo<UnitOfMeasurement>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "doc-sets" => (await Repo<DocSet>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "project-characteristics" => (await Repo<ProjectCharacteristic>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<DirectoryItem?> DispatchGetById(string resource, int id, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => await Repo<PhysicalVolumeKfo>().GetByIdAsync(id, ct),
            "physical-volumes-ber" => await Repo<PhysicalVolumeBer>().GetByIdAsync(id, ct),
            "equipment" => await Repo<Equipment>().GetByIdAsync(id, ct),
            "labor-intensities" => await Repo<LaborIntensity>().GetByIdAsync(id, ct),
            "iic-work-directions" => await Repo<IicWorkDirection>().GetByIdAsync(id, ct),
            "units-of-measurement" => await Repo<UnitOfMeasurement>().GetByIdAsync(id, ct),
            "doc-sets" => await Repo<DocSet>().GetByIdAsync(id, ct),
            "project-characteristics" => await Repo<ProjectCharacteristic>().GetByIdAsync(id, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<DirectoryItem> DispatchCreate(string resource, DirectoryWriteRequest request, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => await Repo<PhysicalVolumeKfo>().CreateAsync(Prepare(new PhysicalVolumeKfo(), request), ct),
            "physical-volumes-ber" => await Repo<PhysicalVolumeBer>().CreateAsync(Prepare(new PhysicalVolumeBer(), request), ct),
            "equipment" => await Repo<Equipment>().CreateAsync(Prepare(new Equipment(), request), ct),
            "labor-intensities" => await Repo<LaborIntensity>().CreateAsync(Prepare(new LaborIntensity(), request), ct),
            "iic-work-directions" => await Repo<IicWorkDirection>().CreateAsync(Prepare(new IicWorkDirection(), request), ct),
            "units-of-measurement" => await Repo<UnitOfMeasurement>().CreateAsync(Prepare(new UnitOfMeasurement(), request), ct),
            "doc-sets" => await Repo<DocSet>().CreateAsync(Prepare(new DocSet(), request), ct),
            "project-characteristics" => await Repo<ProjectCharacteristic>().CreateAsync(Prepare(new ProjectCharacteristic(), request), ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<DirectoryItem?> DispatchUpdate(string resource, int id, DirectoryWriteRequest request, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => await UpdateTyped<PhysicalVolumeKfo>(id, request, ct),
            "physical-volumes-ber" => await UpdateTyped<PhysicalVolumeBer>(id, request, ct),
            "equipment" => await UpdateTyped<Equipment>(id, request, ct),
            "labor-intensities" => await UpdateTyped<LaborIntensity>(id, request, ct),
            "iic-work-directions" => await UpdateTyped<IicWorkDirection>(id, request, ct),
            "units-of-measurement" => await UpdateTyped<UnitOfMeasurement>(id, request, ct),
            "doc-sets" => await UpdateTyped<DocSet>(id, request, ct),
            "project-characteristics" => await UpdateTyped<ProjectCharacteristic>(id, request, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<DirectoryItem?> UpdateTyped<T>(int id, DirectoryWriteRequest request, CancellationToken ct) where T : DirectoryItem, new()
    {
        var existing = await Repo<T>().GetByIdAsync(id, ct);
        if (existing is null)
            return null;

        var updated = Prepare(existing, request);
        return await Repo<T>().UpdateAsync(updated, ct);
    }

    private Task<bool> DispatchArchive(string resource, int id, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => Repo<PhysicalVolumeKfo>().ArchiveAsync(id, ct),
            "physical-volumes-ber" => Repo<PhysicalVolumeBer>().ArchiveAsync(id, ct),
            "equipment" => Repo<Equipment>().ArchiveAsync(id, ct),
            "labor-intensities" => Repo<LaborIntensity>().ArchiveAsync(id, ct),
            "iic-work-directions" => Repo<IicWorkDirection>().ArchiveAsync(id, ct),
            "units-of-measurement" => Repo<UnitOfMeasurement>().ArchiveAsync(id, ct),
            "doc-sets" => Repo<DocSet>().ArchiveAsync(id, ct),
            "project-characteristics" => Repo<ProjectCharacteristic>().ArchiveAsync(id, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private Task<bool> DispatchUnarchive(string resource, int id, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => Repo<PhysicalVolumeKfo>().UnarchiveAsync(id, ct),
            "physical-volumes-ber" => Repo<PhysicalVolumeBer>().UnarchiveAsync(id, ct),
            "equipment" => Repo<Equipment>().UnarchiveAsync(id, ct),
            "labor-intensities" => Repo<LaborIntensity>().UnarchiveAsync(id, ct),
            "iic-work-directions" => Repo<IicWorkDirection>().UnarchiveAsync(id, ct),
            "units-of-measurement" => Repo<UnitOfMeasurement>().UnarchiveAsync(id, ct),
            "doc-sets" => Repo<DocSet>().UnarchiveAsync(id, ct),
            "project-characteristics" => Repo<ProjectCharacteristic>().UnarchiveAsync(id, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private Task<bool> DispatchDelete(string resource, int id, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => Repo<PhysicalVolumeKfo>().DeleteAsync(id, ct),
            "physical-volumes-ber" => Repo<PhysicalVolumeBer>().DeleteAsync(id, ct),
            "equipment" => Repo<Equipment>().DeleteAsync(id, ct),
            "labor-intensities" => Repo<LaborIntensity>().DeleteAsync(id, ct),
            "iic-work-directions" => Repo<IicWorkDirection>().DeleteAsync(id, ct),
            "units-of-measurement" => Repo<UnitOfMeasurement>().DeleteAsync(id, ct),
            "doc-sets" => Repo<DocSet>().DeleteAsync(id, ct),
            "project-characteristics" => Repo<ProjectCharacteristic>().DeleteAsync(id, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private Task<bool> DispatchSortOrder(string resource, int id, SortOrderRequest request, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => Repo<PhysicalVolumeKfo>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "physical-volumes-ber" => Repo<PhysicalVolumeBer>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "equipment" => Repo<Equipment>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "labor-intensities" => Repo<LaborIntensity>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "iic-work-directions" => Repo<IicWorkDirection>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "units-of-measurement" => Repo<UnitOfMeasurement>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "doc-sets" => Repo<DocSet>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            "project-characteristics" => Repo<ProjectCharacteristic>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }
}
