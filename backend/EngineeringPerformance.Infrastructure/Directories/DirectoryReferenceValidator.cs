using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Domain.Common;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Directories;

/// <summary>Проверка FK-ссылок справочников перед сохранением (A-04).</summary>
internal sealed class DirectoryReferenceValidator
{
    private readonly AppDbContext _context;

    public DirectoryReferenceValidator(AppDbContext context)
    {
        _context = context;
    }

    public async Task ValidateAsync(string resource, DirectoryWriteRequest request, int? entityId, CancellationToken cancellationToken)
    {
        if (request.ParentId is int parentId)
        {
            if (entityId.HasValue && parentId == entityId.Value)
                throw new InvalidOperationException("ParentId cannot reference the same entry.");

            var parentExists = await ParentExistsAsync(resource, parentId, cancellationToken);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent entry {parentId} not found in '{resource}'.");
        }

        switch (resource.ToLowerInvariant())
        {
            case "physical-volumes-kfo":
                await ValidateKfoAsync(request, cancellationToken);
                break;
            case "physical-volumes-ber":
                await ValidateBerAsync(request, cancellationToken);
                break;
            case "equipment":
                await ValidateEquipmentAsync(request, cancellationToken);
                break;
            case "labor-intensities":
                await ValidateLaborIntensityAsync(request, cancellationToken);
                break;
            case "iic-work-directions":
                await ValidateIicWorkDirectionAsync(request, cancellationToken);
                break;
        }
    }

    private async Task<bool> ParentExistsAsync(string resource, int parentId, CancellationToken cancellationToken) =>
        resource.ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => await _context.PhysicalVolumesKfo.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "physical-volumes-ber" => await _context.PhysicalVolumesBer.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "equipment" => await _context.Equipments.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "labor-intensities" => await _context.LaborIntensities.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "iic-work-directions" => await _context.IicWorkDirections.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "units-of-measurement" => await _context.UnitsOfMeasurement.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            "project-characteristics" => await _context.ProjectCharacteristics.AsNoTracking().AnyAsync(x => x.Id == parentId, cancellationToken),
            _ => throw new KeyNotFoundException($"Unknown directory resource '{resource}'."),
        };

    private async Task ValidateKfoAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateClassifierIdAsync(request, cancellationToken);
        await ValidateUnitIdAsync(request, cancellationToken);
        await ValidateOptionalIicNormIdAsync(request, cancellationToken);
    }

    private async Task ValidateBerAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateClassifierIdAsync(request, cancellationToken);
        await ValidateUnitIdAsync(request, cancellationToken);
        await ValidatePhysicalVolumeKfoIdAsync(request, cancellationToken);
    }

    private async Task ValidatePhysicalVolumeKfoIdAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        if (TryGetInt(request, "physicalVolumeKfoId", out var kfoId)
            && !await _context.PhysicalVolumesKfo.AsNoTracking().AnyAsync(x => x.Id == kfoId, cancellationToken))
        {
            throw new KeyNotFoundException($"PhysicalVolumeKfo {kfoId} not found.");
        }
    }

    private async Task ValidateEquipmentAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateClassifierIdAsync(request, cancellationToken);
        await ValidateUnitIdAsync(request, cancellationToken);

        if (!TryGetInt(request, "projectId", out var projectId))
            throw new InvalidOperationException("Equipment requires properties.projectId.");

        if (!await _context.Projects.AsNoTracking().AnyAsync(x => x.Id == projectId, cancellationToken))
            throw new KeyNotFoundException($"Project {projectId} not found.");

        if (request.ParentId is int parentId)
        {
            var parentProjectId = await _context.Equipments.AsNoTracking()
                .Where(x => x.Id == parentId)
                .Select(x => x.ProjectId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentProjectId != projectId)
                throw new InvalidOperationException("Equipment parent must belong to the same project.");
        }
    }

    private async Task ValidateLaborIntensityAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        if (TryGetInt(request, "iicWorkDirectionId", out var directionId)
            && !await _context.IicWorkDirections.AsNoTracking().AnyAsync(x => x.Id == directionId, cancellationToken))
        {
            throw new KeyNotFoundException($"IicWorkDirection {directionId} not found.");
        }
    }

    private async Task ValidateIicWorkDirectionAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateUnitIdAsync(request, cancellationToken);
    }

    private async Task ValidateClassifierIdAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        if (TryGetInt(request, "classifierId", out var classifierId)
            && !await _context.IicWorkDirections.AsNoTracking().AnyAsync(x => x.Id == classifierId, cancellationToken))
        {
            throw new KeyNotFoundException($"IicWorkDirection (ClassifierId) {classifierId} not found.");
        }
    }

    private async Task ValidateUnitIdAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        if (TryGetInt(request, "unitId", out var unitId)
            && !await _context.UnitsOfMeasurement.AsNoTracking().AnyAsync(x => x.Id == unitId, cancellationToken))
        {
            throw new KeyNotFoundException($"UnitOfMeasurement {unitId} not found.");
        }
    }

    private async Task ValidateOptionalIicNormIdAsync(DirectoryWriteRequest request, CancellationToken cancellationToken)
    {
        if (TryGetInt(request, "iicNormId", out var normId)
            && !await _context.IicWorkDirectionNorms.AsNoTracking().AnyAsync(x => x.Id == normId, cancellationToken))
        {
            throw new KeyNotFoundException($"IicWorkDirectionNorm {normId} not found.");
        }
    }

    private static bool TryGetInt(DirectoryWriteRequest request, string key, out int value)
    {
        value = default;
        if (request.Properties is null || !request.Properties.TryGetValue(key, out var element))
            return false;

        if (element.ValueKind == System.Text.Json.JsonValueKind.Null)
            return false;

        if (element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetInt32(out value))
            return true;

        return int.TryParse(element.ToString(), out value);
    }
}
