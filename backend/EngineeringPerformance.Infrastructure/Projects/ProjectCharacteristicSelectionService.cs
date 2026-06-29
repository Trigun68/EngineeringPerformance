using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectCharacteristicSelectionService : IProjectCharacteristicSelectionService
{
    private readonly AppDbContext _context;

    public ProjectCharacteristicSelectionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProjectCharacteristicSelectionDto>> GetByProjectAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var characteristics = await _context.ProjectCharacteristics
            .AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
            })
            .ToListAsync(cancellationToken);

        var options = await _context.ProjectCharacteristicOptions
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.CharacteristicId,
                x.Name,
                x.ValuePercent,
            })
            .ToListAsync(cancellationToken);

        var selectedByCharacteristic = await _context.ProjectCharacteristicSelections
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .ToDictionaryAsync(x => x.CharacteristicId, x => x.OptionId, cancellationToken);

        var optionsByCharacteristic = options
            .GroupBy(x => x.CharacteristicId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ProjectCharacteristicOptionDto>)g
                    .Select(x => new ProjectCharacteristicOptionDto(x.Id, x.Name, x.ValuePercent))
                    .ToList());

        return characteristics
            .Select(characteristic =>
            {
                optionsByCharacteristic.TryGetValue(characteristic.Id, out var characteristicOptions);
                characteristicOptions ??= [];

                selectedByCharacteristic.TryGetValue(characteristic.Id, out var selectedOptionId);
                var selected = characteristicOptions.FirstOrDefault(x => x.Id == selectedOptionId);

                return new ProjectCharacteristicSelectionDto(
                    CharacteristicId: characteristic.Id,
                    CharacteristicName: characteristic.Name,
                    SelectedOptionId: selected?.Id,
                    SelectedOptionName: selected?.Name,
                    SelectedValuePercent: selected?.ValuePercent,
                    Options: characteristicOptions);
            })
            .ToList();
    }

    public async Task SaveByProjectAsync(
        int projectId,
        IReadOnlyList<ProjectCharacteristicSelectionWriteDto> selections,
        CancellationToken cancellationToken = default)
    {
        var projectExists = await _context.Projects
            .AsNoTracking()
            .AnyAsync(x => x.Id == projectId && !x.IsArchived, cancellationToken);

        if (!projectExists)
            throw new InvalidOperationException($"Project #{projectId} not found.");

        var normalized = selections
            .GroupBy(x => x.CharacteristicId)
            .Select(g => g.Last())
            .ToList();

        if (normalized.Count > 0)
        {
            var optionIds = normalized.Select(x => x.OptionId).Distinct().ToList();
            var optionMap = await _context.ProjectCharacteristicOptions
                .AsNoTracking()
                .Where(x => optionIds.Contains(x.Id))
                .Select(x => new { x.Id, x.CharacteristicId })
                .ToDictionaryAsync(x => x.Id, x => x.CharacteristicId, cancellationToken);

            foreach (var selection in normalized)
            {
                if (!optionMap.TryGetValue(selection.OptionId, out var characteristicId))
                    throw new InvalidOperationException($"Option #{selection.OptionId} not found.");

                if (characteristicId != selection.CharacteristicId)
                {
                    throw new InvalidOperationException(
                        $"Option #{selection.OptionId} does not belong to characteristic #{selection.CharacteristicId}.");
                }
            }
        }

        var existing = await _context.ProjectCharacteristicSelections
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var incomingCharacteristicIds = normalized
            .Select(x => x.CharacteristicId)
            .ToHashSet();

        foreach (var row in existing.Where(x => !incomingCharacteristicIds.Contains(x.CharacteristicId)))
            _context.ProjectCharacteristicSelections.Remove(row);

        foreach (var selection in normalized)
        {
            var row = existing.FirstOrDefault(x => x.CharacteristicId == selection.CharacteristicId);
            if (row is null)
            {
                _context.ProjectCharacteristicSelections.Add(new Domain.Entities.ProjectCharacteristicSelection
                {
                    ProjectId = projectId,
                    CharacteristicId = selection.CharacteristicId,
                    OptionId = selection.OptionId,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else if (row.OptionId != selection.OptionId)
            {
                row.OptionId = selection.OptionId;
                row.ModifiedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
