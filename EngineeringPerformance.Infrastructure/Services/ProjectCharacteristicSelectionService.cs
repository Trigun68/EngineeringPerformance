using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed record ProjectCharacteristicSelectionRow(
    int Id,
    int ProjectId,
    int CharacteristicId,
    string CharacteristicName,
    int OptionId,
    string OptionName,
    decimal? ValuePercent);

public sealed class ProjectCharacteristicSelectionService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ProjectCharacteristicSelectionService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<ProjectCharacteristicSelectionRow>> GetByProjectAsync(int projectId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProjectCharacteristicSelections
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Characteristic!.SortOrder)
            .ThenBy(s => s.Characteristic!.Name)
            .Select(s => new ProjectCharacteristicSelectionRow(
                s.Id,
                s.ProjectId,
                s.CharacteristicId,
                s.Characteristic!.Name,
                s.OptionId,
                s.Option!.Name,
                s.Option!.ValuePercent))
            .ToListAsync();
    }

    public async Task<ProjectCharacteristicSelection> CreateAsync(int projectId, int characteristicId, int optionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = new ProjectCharacteristicSelection
        {
            ProjectId = projectId,
            CharacteristicId = characteristicId,
            OptionId = optionId,
            CreatedAt = DateTime.UtcNow,
        };
        context.ProjectCharacteristicSelections.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(int id, int characteristicId, int optionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.ProjectCharacteristicSelections.FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException($"ProjectCharacteristicSelection Id={id} not found.");
        entity.CharacteristicId = characteristicId;
        entity.OptionId = optionId;
        entity.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.ProjectCharacteristicSelections.FirstOrDefaultAsync(s => s.Id == id);
        if (entity is null) return;
        context.ProjectCharacteristicSelections.Remove(entity);
        await context.SaveChangesAsync();
    }
}
