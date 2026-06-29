using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectDirectoryService : DirectoryServiceBase<Project>
{
    public ProjectDirectoryService(IDbContextFactory<AppDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    protected override DbSet<Project> GetDbSet(AppDbContext context) => context.Projects;

    public override async Task<IReadOnlyList<Project>> GetAllAsync(bool includeArchived = false)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var query = GetDbSet(context).AsNoTracking();

        if (!includeArchived)
            query = query.Where(e => !e.IsArchived);

        var projects = await query
            .OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
            .ToListAsync();

        var versionCounts = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .GroupBy(v => v.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        foreach (var project in projects)
        {
            project.VersionCount = versionCounts.TryGetValue(project.Id, out var count) ? count : 0;
        }

        return projects;
    }
}
