using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectAnalysisVersionDirectoryService
    : DirectoryServiceBase<ProjectAnalysisVersion>, IProjectAnalysisVersionQueryService
{
    public ProjectAnalysisVersionDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<ProjectAnalysisVersion> GetDbSet(AppDbContext context) => context.ProjectAnalysisVersions;

    // V2-B.1: при создании версии анализа фиксируем снимок норм, действующих на момент CreatedAt.
    // Без этого Z-score-отчёты «плыли» бы при последующей правке IicWorkDirectionNorms.
    public override async Task<ProjectAnalysisVersion> CreateAsync(ProjectAnalysisVersion item)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        item.CreatedAt = DateTime.UtcNow;
        context.ProjectAnalysisVersions.Add(item);
        await context.SaveChangesAsync();

        await PopulateSnapshotInternalAsync(context, item.Id, item.CreatedAt);
        await context.SaveChangesAsync();

        return item;
    }

    /// <summary>
    /// Перестраивает снимок норм для существующей версии: удаляет старые строки и пересоздаёт
    /// по справочнику IicWorkDirectionNorms на момент <see cref="ProjectAnalysisVersion.CreatedAt"/>.
    /// Используется при ручном восстановлении данных и в интеграционных тестах.
    /// </summary>
    public async Task RebuildSnapshotAsync(int versionId, CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        var version = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

        if (version is null)
        {
            throw new InvalidOperationException($"ProjectAnalysisVersion with Id={versionId} not found.");
        }

        var existing = context.AnalysisVersionSnapshots.Where(s => s.ProjectAnalysisVersionId == versionId);
        context.AnalysisVersionSnapshots.RemoveRange(existing);

        await PopulateSnapshotInternalAsync(context, versionId, version.CreatedAt, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    // Выбираем по одной норме на WorkDirectionId, действующей на versionCreatedAt:
    // EffectiveFrom <= versionCreatedAt AND (EffectiveTo IS NULL OR EffectiveTo >= versionCreatedAt).
    // При перекрытиях (грязные данные) — самая свежая по EffectiveFrom: тот же детерминизм,
    // что и в PerformanceService, иначе Z-score менялся бы между прогонами.
    private static async Task PopulateSnapshotInternalAsync(
        AppDbContext context,
        int versionId,
        DateTime versionCreatedAt,
        CancellationToken cancellationToken = default)
    {
        var activeNorms = await context.IicWorkDirectionNorms
            .AsNoTracking()
            .Where(n => n.EffectiveFrom <= versionCreatedAt
                        && (n.EffectiveTo == null || n.EffectiveTo >= versionCreatedAt))
            .ToListAsync(cancellationToken);

        var pickedByDirection = activeNorms
            .GroupBy(n => n.WorkDirectionId)
            .Select(g => g.OrderByDescending(n => n.EffectiveFrom).First());

        foreach (var norm in pickedByDirection)
        {
            context.AnalysisVersionSnapshots.Add(new AnalysisVersionSnapshot
            {
                ProjectAnalysisVersionId = versionId,
                WorkDirectionId = norm.WorkDirectionId,
                NormId = norm.Id
            });
        }
    }

    public override async Task<IReadOnlyList<ProjectAnalysisVersion>> GetAllAsync(bool includeArchived = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        IQueryable<ProjectAnalysisVersion> query = GetDbSet(context)
            .AsNoTracking()
            .Include(v => v.Project)
            .Include(v => v.PhysicalVolumeType);

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public override async Task<ProjectAnalysisVersion?> GetByIdAsync(int id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await GetDbSet(context)
            .AsNoTracking()
            .Include(v => v.Project)
            .Include(v => v.PhysicalVolumeType)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IReadOnlyList<ProjectAnalysisVersion>> GetByProjectAsync(
        int projectId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await ContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<ProjectAnalysisVersion> query = GetDbSet(context)
            .AsNoTracking()
            .Include(v => v.PhysicalVolumeType)
            .Where(v => v.ProjectId == projectId);

        if (!includeArchived)
            query = query.Where(v => !v.IsArchived);

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
