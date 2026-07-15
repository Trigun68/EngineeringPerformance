using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Analytics;

public interface IAggregatedLaborIntensitiesService
{
    Task<AggregatedLaborIntensitiesDto> GetAsync(
        int projectId,
        int versionId,
        AggregationLevel level,
        CancellationToken cancellationToken = default);
}

public sealed class AggregatedLaborIntensitiesService : IAggregatedLaborIntensitiesService
{
    private readonly AppDbContext _context;

    public AggregatedLaborIntensitiesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AggregatedLaborIntensitiesDto> GetAsync(
        int projectId,
        int versionId,
        AggregationLevel level,
        CancellationToken cancellationToken = default)
    {
        var ppvs = await _context.ProjectPhysicalVolumes
            .AsNoTracking()
            .Where(pv => pv.ProjectId == projectId && pv.ProjectAnalysisVersionId == versionId)
            .ToListAsync(cancellationToken);

        IEnumerable<AggregatedLaborIntensityRowDto> rows = level switch
        {
            AggregationLevel.Total =>
            [
                new AggregatedLaborIntensityRowDto(
                    "total",
                    "Итого",
                    ppvs.Sum(p => p.LaborIntensityBase ?? 0),
                    ppvs.Sum(p => p.LaborIntensityPessimistic ?? 0),
                    ppvs.Sum(p => p.LaborIntensityOptimistic ?? 0)),
            ],
            AggregationLevel.ByObject => ppvs
                .GroupBy(p => (Code: p.FacilityCode ?? string.Empty, Name: p.FacilityName ?? string.Empty))
                .OrderBy(g => g.Key.Code)
                .Select(g => new AggregatedLaborIntensityRowDto(
                    g.Key.Code,
                    g.Key.Name,
                    g.Sum(p => p.LaborIntensityBase ?? 0),
                    g.Sum(p => p.LaborIntensityPessimistic ?? 0),
                    g.Sum(p => p.LaborIntensityOptimistic ?? 0))),
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };

        return new AggregatedLaborIntensitiesDto(level, rows.ToList());
    }
}
