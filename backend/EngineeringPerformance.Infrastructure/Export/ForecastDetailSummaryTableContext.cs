using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Export;

public sealed class ForecastDetailSummaryTableContext(AppDbContext context) : IForecastDetailSummaryTableContext
{
    public async Task<(string ProjectCode, string VersionName, IReadOnlyList<IicDirectionNodeDto> Directions)> LoadAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.Id == versionId && v.ProjectId == projectId)
            .Select(v => new { v.VersionName, ProjectCode = v.Project.Code })
            .FirstOrDefaultAsync(cancellationToken);

        if (metadata is null)
            throw new InvalidOperationException($"Project version {versionId} not found for project {projectId}.");

        var directions = await context.IicWorkDirections
            .AsNoTracking()
            .Include(d => d.Unit)
            .Where(d => !d.IsArchived)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new IicDirectionNodeDto(
                d.Id,
                d.ParentId,
                d.Name,
                d.Code,
                d.SortOrder,
                d.Unit != null ? d.Unit.Name : null))
            .ToListAsync(cancellationToken);

        return (metadata.ProjectCode, metadata.VersionName, directions);
    }
}
