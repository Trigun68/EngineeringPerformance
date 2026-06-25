using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Directories;

public sealed class DirectoryFacade : IDirectoryFacade
{
    private readonly AppDbContext _context;
    private readonly ICacheInvalidator _cacheInvalidator;

    public DirectoryFacade(AppDbContext context, ICacheInvalidator cacheInvalidator)
    {
        _context = context;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<IReadOnlyList<DirectoryEntryDto>> ListAsync(string resource, bool includeArchived, CancellationToken cancellationToken)
    {
        return await DispatchList(resource, includeArchived, cancellationToken);
    }

    public async Task<IReadOnlyList<DirectoryEntryDto>> GetChildrenAsync(string resource, int? parentId, bool includeArchived, CancellationToken cancellationToken)
    {
        return await DispatchChildren(resource, parentId, includeArchived, cancellationToken);
    }

    public Task<IReadOnlyList<DirectoryTreeNodeDto>> GetTreeAsync(string resource, bool includeArchived, CancellationToken cancellationToken) =>
        resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => MapTree<PhysicalVolumeKfo>(includeArchived, cancellationToken),
            "physical-volumes-ber" => MapTree<PhysicalVolumeBer>(includeArchived, cancellationToken),
            "equipment" => MapTree<Equipment>(includeArchived, cancellationToken),
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
        var created = await DispatchCreate(resource, request, cancellationToken);
        await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return DirectoryEntityMapper.ToDto(created);
    }

    public async Task<DirectoryEntryDto?> UpdateAsync(string resource, int id, DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        var updated = await DispatchUpdate(resource, id, request, cancellationToken);
        if (updated is not null)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return updated is null ? null : DirectoryEntityMapper.ToDto(updated);
    }

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
        var result = await DispatchDelete(resource, id, cancellationToken);
        if (result)
            await _cacheInvalidator.InvalidateAsync(cancellationToken);
        return result;
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

    private async Task<IReadOnlyList<DirectoryTreeNodeDto>> MapTree<T>(bool includeArchived, CancellationToken cancellationToken)
        where T : DirectoryItem, new()
    {
        var items = await Repo<T>().GetAllAsync(includeArchived, cancellationToken);
        return DirectoryEntityMapper.BuildTree(items);
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
        if (merged is Equipment equipment)
            equipment.CatalogKeyHash = EquipmentCatalogKeyHasher.Compute(equipment.Name, equipment.UnitId);
        return merged;
    }

    private async Task<IReadOnlyList<DirectoryEntryDto>> DispatchList(string resource, bool includeArchived, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => (await Repo<PhysicalVolumeKfo>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "physical-volumes-ber" => (await Repo<PhysicalVolumeBer>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "equipment" => (await Repo<Equipment>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "labor-intensities" => (await Repo<LaborIntensity>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "iic-work-directions" => (await Repo<IicWorkDirection>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "units-of-measurement" => (await Repo<UnitOfMeasurement>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "project-characteristics" => (await Repo<ProjectCharacteristic>().GetAllAsync(includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }

    private async Task<IReadOnlyList<DirectoryEntryDto>> DispatchChildren(string resource, int? parentId, bool includeArchived, CancellationToken ct)
    {
        EnsureResource(resource);
        return resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => (await Repo<PhysicalVolumeKfo>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "physical-volumes-ber" => (await Repo<PhysicalVolumeBer>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "equipment" => (await Repo<Equipment>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "labor-intensities" => (await Repo<LaborIntensity>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "iic-work-directions" => (await Repo<IicWorkDirection>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
            "units-of-measurement" => (await Repo<UnitOfMeasurement>().GetChildrenAsync(parentId, includeArchived, ct)).Select(DirectoryEntityMapper.ToDto).ToList(),
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
            "project-characteristics" => Repo<ProjectCharacteristic>().UpdateSortOrderAsync(id, request.ParentId, request.SortOrder, ct),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'.")
        };
    }
}
