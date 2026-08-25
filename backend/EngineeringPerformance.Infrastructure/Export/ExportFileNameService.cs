using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Export;

public sealed class ExportFileNameService(AppDbContext context) : IExportFileNameService
{
    public Task<string> ForVersionAsync(
        int projectId,
        int versionId,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default) =>
        ForVersionsAsync(
            projectId,
            [versionId],
            module,
            extension,
            view,
            qualifier,
            cancellationToken);

    public async Task<string> ForVersionsAsync(
        int projectId,
        IReadOnlyList<int> versionIds,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default)
    {
        var (code, name) = await LoadProjectAsync(projectId, cancellationToken);
        var versionLabel = await LoadVersionLabelAsync(projectId, versionIds, cancellationToken);
        return ExportFileNameBuilder.Build(module, extension, view, qualifier, code, name, versionLabel);
    }

    public async Task<string> ForProjectAsync(
        int projectId,
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        CancellationToken cancellationToken = default)
    {
        var (code, name) = await LoadProjectAsync(projectId, cancellationToken);
        return ExportFileNameBuilder.Build(module, extension, view, qualifier, code, name);
    }

    public async Task<string> ForPerformanceReportAsync(
        int projectId,
        int? planVersionId,
        int? factVersionId,
        CancellationToken cancellationToken = default)
    {
        var ids = new List<int>(2);
        if (planVersionId is int planId)
            ids.Add(planId);
        if (factVersionId is int factId)
            ids.Add(factId);

        if (ids.Count == 0)
        {
            var primary = await LoadPrimaryPlanFactIdsAsync(projectId, cancellationToken);
            ids.AddRange(primary);
        }

        if (ids.Count == 0)
        {
            return await ForProjectAsync(
                projectId,
                ExportFileNameBuilder.Modules.Report,
                "pdf",
                cancellationToken: cancellationToken);
        }

        return await ForVersionsAsync(
            projectId,
            ids,
            ExportFileNameBuilder.Modules.Report,
            "pdf",
            cancellationToken: cancellationToken);
    }

    private async Task<(string Code, string Name)> LoadProjectAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Code, p.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
            throw new InvalidOperationException($"Project {projectId} not found.");

        return (project.Code, project.Name);
    }

    private async Task<string?> LoadVersionLabelAsync(
        int projectId,
        IReadOnlyList<int> versionIds,
        CancellationToken cancellationToken)
    {
        if (versionIds.Count == 0)
            return null;

        var orderedIds = versionIds.Distinct().ToList();
        if (orderedIds.Count == 0)
            return null;

        var names = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId && orderedIds.Contains(v.Id))
            .Select(v => new { v.Id, v.VersionName })
            .ToListAsync(cancellationToken);

        var byId = names.ToDictionary(v => v.Id, v => v.VersionName);
        var labels = new List<string?>(orderedIds.Count);
        foreach (var id in orderedIds)
        {
            if (byId.TryGetValue(id, out var versionName))
                labels.Add(versionName);
        }

        if (labels.Count == 0 && orderedIds.Count == 1)
            throw new InvalidOperationException(
                $"Project version {orderedIds[0]} not found for project {projectId}.");

        return ExportFileNameBuilder.CombineVersions([.. labels]);
    }

    private async Task<IReadOnlyList<int>> LoadPrimaryPlanFactIdsAsync(
        int projectId,
        CancellationToken cancellationToken)
    {
        var versions = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId && !v.IsArchived)
            .Select(v => new { v.Id, v.Direction, v.IsPrimary, v.CreatedAt })
            .ToListAsync(cancellationToken);

        var planId = versions
            .Where(v => v.Direction == AnalysisDirection.Plan)
            .OrderByDescending(v => v.IsPrimary)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => (int?)v.Id)
            .FirstOrDefault();
        var factId = versions
            .Where(v => v.Direction == AnalysisDirection.Fact)
            .OrderByDescending(v => v.IsPrimary)
            .ThenByDescending(v => v.CreatedAt)
            .Select(v => (int?)v.Id)
            .FirstOrDefault();

        return new[] { planId, factId }.OfType<int>().Distinct().ToArray();
    }
}
