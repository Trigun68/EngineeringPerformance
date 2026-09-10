using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Audit;
using EngineeringPerformance.Application.Projects;
using EngineeringPerformance.Domain.Entities;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Projects;

public sealed class ProjectVersionPlanningParameterService(
    AppDbContext context,
    IAnalysisVersionRecalculationOrchestrator recalcOrchestrator,
    ProjectsPageRawDataLoader rawLoader,
    IUserActionLogger actionLogger) : IProjectVersionPlanningParameterService
{
    private readonly AppDbContext _context = context;
    private readonly IAnalysisVersionRecalculationOrchestrator _recalcOrchestrator = recalcOrchestrator;
    private readonly ProjectsPageRawDataLoader _rawLoader = rawLoader;
    private readonly IUserActionLogger _actionLogger = actionLogger;

    public async Task<IReadOnlyList<ProjectVersionPlanningParameterDto>> ListAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionExistsAsync(projectId, versionId, cancellationToken);
        await SeedForVersionAsync(versionId, cancellationToken);

        var rows = await _context.ProjectVersionPlanningParameters.AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .Join(
                _context.IicWorkDirections.AsNoTracking(),
                p => p.IicWorkDirectionId,
                d => d.Id,
                (p, d) => new { p, d })
            .GroupJoin(
                _context.UnitsOfMeasurement.AsNoTracking(),
                x => x.d.UnitId,
                u => u.Id,
                (x, units) => new { x.p, x.d, unit = units.FirstOrDefault() })
            .OrderBy(x => x.d.SortOrder)
            .ThenBy(x => x.d.Name)
            .Select(x => new ProjectVersionPlanningParameterDto(
                x.p.Id,
                x.p.IicWorkDirectionId,
                x.d.ParentId,
                x.d.SortOrder,
                x.d.Code,
                x.d.Name,
                x.unit != null ? x.unit.Name : null,
                x.p.ZPessimistic,
                x.p.ZBase,
                x.p.ZOptimistic,
                x.p.ZActual,
                0m))
            .ToListAsync(cancellationToken);

        var weights = await LoadLaborHoursWeightsAsync(projectId, versionId, cancellationToken);
        if (weights.Count == 0)
            return rows;

        return rows
            .Select(r => r with
            {
                LaborHoursWeight = weights.GetValueOrDefault(r.IicWorkDirectionId),
            })
            .ToList();
    }

    public async Task SaveAsync(
        int projectId,
        int versionId,
        SaveProjectVersionPlanningParametersRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureVersionExistsAsync(projectId, versionId, cancellationToken);
        await SeedForVersionAsync(versionId, cancellationToken);

        var existing = await _context.ProjectVersionPlanningParameters
            .Where(p => p.ProjectAnalysisVersionId == versionId)
            .ToDictionaryAsync(p => p.IicWorkDirectionId, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!existing.TryGetValue(item.IicWorkDirectionId, out var row))
                continue;

            row.ZPessimistic = item.ZPessimistic;
            row.ZBase = item.ZBase;
            row.ZOptimistic = item.ZOptimistic;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _actionLogger.LogAsync(
            UserActionType.PlanningParameterEdit,
            entityType: "ProjectVersionPlanningParameter",
            entityId: versionId,
            description: $"Updated planning parameters for version {versionId} ({request.Items.Count} items)",
            cancellationToken: cancellationToken);
        await _recalcOrchestrator.RecalculateVersionAsync(projectId, versionId, cancellationToken);
    }

    public async Task SeedForVersionAsync(int versionId, CancellationToken cancellationToken)
    {
        var hasAny = await _context.ProjectVersionPlanningParameters
            .AnyAsync(p => p.ProjectAnalysisVersionId == versionId, cancellationToken);
        if (hasAny)
            return;

        var directions = await _context.IicWorkDirections.AsNoTracking()
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        foreach (var directionId in directions)
        {
            _context.ProjectVersionPlanningParameters.Add(new ProjectVersionPlanningParameter
            {
                ProjectAnalysisVersionId = versionId,
                IicWorkDirectionId = directionId,
                ZPessimistic = 0.5m,
                ZBase = 0m,
                ZOptimistic = -0.5m,
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task CopyFromVersionAsync(int sourceVersionId, int targetVersionId, CancellationToken cancellationToken = default)
    {
        await SeedForVersionAsync(sourceVersionId, cancellationToken);
        await SeedForVersionAsync(targetVersionId, cancellationToken);

        var sourceRows = await _context.ProjectVersionPlanningParameters.AsNoTracking()
            .Where(p => p.ProjectAnalysisVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        var targetRows = await _context.ProjectVersionPlanningParameters
            .Where(p => p.ProjectAnalysisVersionId == targetVersionId)
            .ToDictionaryAsync(p => p.IicWorkDirectionId, cancellationToken);

        foreach (var source in sourceRows)
        {
            if (!targetRows.TryGetValue(source.IicWorkDirectionId, out var target))
                continue;

            target.ZPessimistic = source.ZPessimistic;
            target.ZBase = source.ZBase;
            target.ZOptimistic = source.ZOptimistic;
            target.ZActual = source.ZActual;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Веса диалога = сохранённые базовые часы плана по направлению.
    /// </summary>
    private async Task<Dictionary<int, decimal>> LoadLaborHoursWeightsAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken)
    {
        var bundle = await _rawLoader.LoadAsync(projectId, versionId, cancellationToken);
        var hours = ProjectsPageGridBuilder.ComputeLivePlanHoursByDirection(bundle);
        return hours.ToDictionary(kv => kv.Key, kv => kv.Value.Base ?? 0m);
    }

    private async Task EnsureVersionExistsAsync(int projectId, int versionId, CancellationToken cancellationToken)
    {
        var exists = await _context.ProjectAnalysisVersions.AsNoTracking()
            .AnyAsync(v => v.ProjectId == projectId && v.Id == versionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Version {versionId} not found for project {projectId}.");
    }
}
