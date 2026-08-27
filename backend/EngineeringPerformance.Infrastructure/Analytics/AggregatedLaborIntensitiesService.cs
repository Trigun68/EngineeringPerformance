using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Analytics;

public interface IAggregatedLaborIntensitiesService
{
    Task<AggregatedLaborIntensitiesDto> GetAsync(
        int projectId,
        int versionId,
        AggregationLevel level,
        CancellationToken cancellationToken = default);
}

public sealed class AggregatedLaborIntensitiesService(
    ProjectsPageRawDataLoader rawLoader) : IAggregatedLaborIntensitiesService
{
    public async Task<AggregatedLaborIntensitiesDto> GetAsync(
        int projectId,
        int versionId,
        AggregationLevel level,
        CancellationToken cancellationToken = default)
    {
        var bundle = await rawLoader.LoadAsync(projectId, versionId, cancellationToken);
        var hoursByPpvId = ProjectsPageGridBuilder.ComputeLivePpvHours(bundle);
        var ppvById = bundle.PhysicalVolumes.ToDictionary(pv => pv.Id);

        IEnumerable<AggregatedLaborIntensityRowDto> rows = level switch
        {
            AggregationLevel.Total =>
            [
                SumRow("total", "Итого", hoursByPpvId.Values),
            ],
            AggregationLevel.ByObject => hoursByPpvId
                .Where(kv => ppvById.ContainsKey(kv.Key))
                .GroupBy(kv =>
                {
                    var pv = ppvById[kv.Key];
                    return (Code: pv.FacilityCode ?? string.Empty, Name: pv.FacilityName ?? string.Empty);
                })
                .OrderBy(g => g.Key.Code)
                .Select(g => SumRow(g.Key.Code, g.Key.Name, g.Select(x => x.Value))),
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };

        return new AggregatedLaborIntensitiesDto(level, rows.ToList());
    }

    private static AggregatedLaborIntensityRowDto SumRow(
        string key,
        string label,
        IEnumerable<(decimal? Base, decimal? Optimistic, decimal? Pessimistic)> hours)
    {
        decimal baseSum = 0m, pesSum = 0m, optSum = 0m;
        foreach (var h in hours)
        {
            baseSum += h.Base ?? 0m;
            pesSum += h.Pessimistic ?? 0m;
            optSum += h.Optimistic ?? 0m;
        }

        return new AggregatedLaborIntensityRowDto(key, label, baseSum, pesSum, optSum);
    }
}
