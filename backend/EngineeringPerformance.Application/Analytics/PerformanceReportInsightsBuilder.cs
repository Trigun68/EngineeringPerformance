namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Выводы и топ дисциплин для вкладки «Отчет».
/// Ранжирование влияния: |Z| × subtreeHours. Без kind=data / точности / «не отнесено».
/// </summary>
public static class PerformanceReportInsightsBuilder
{
    public static PerformanceReportInsightsDto Build(
        IReadOnlyList<PerformanceReportMergedDirectionRowDto> directions)
    {
        var leaves = directions
            .Where(d => !directions.Any(c => c.ParentId == d.WorkDirectionId))
            .Where(d => d.FactZScore.HasValue || d.PlanZScore.HasValue)
            .ToList();

        var ranked = leaves
            .Select(r =>
            {
                var z = r.FactZScore ?? r.PlanZScore;
                var hours = r.FactSubtreeHours ?? r.PlanSubtreeHours ?? 0m;
                var volume = r.PhysicalVolume;
                return new PerformanceReportKeyDisciplineDto(
                    WorkDirectionId: r.WorkDirectionId,
                    Name: r.Name,
                    Code: r.Code,
                    ZScore: z,
                    SubtreeHours: hours,
                    InfluenceScore: Math.Abs(z ?? 0m) * hours,
                    PhysicalVolume: volume);
            })
            .Where(r => r.SubtreeHours > 0m && r.ZScore.HasValue)
            .OrderByDescending(r => r.InfluenceScore)
            .ToList();

        var topNegative = ranked
            .Where(r => (r.ZScore ?? 0m) > 0m)
            .Take(3)
            .ToList();
        var topPositive = ranked
            .Where(r => (r.ZScore ?? 0m) < 0m)
            .Take(3)
            .ToList();

        var items = new List<PerformanceReportInsightItemDto>();
        foreach (var row in topNegative)
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "risk",
                Title: $"{row.Name}: Z = {FormatSigned(row.ZScore)}",
                Detail: $"Фокус внимания (влияние |Z|×часы = {row.InfluenceScore:0.0}). Объём: {FormatVolume(row.PhysicalVolume)}."));
        }

        foreach (var row in topPositive)
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "strength",
                Title: $"{row.Name}: Z = {FormatSigned(row.ZScore)}",
                Detail: $"Лучшая практика (влияние |Z|×часы = {row.InfluenceScore:0.0}). Объём: {FormatVolume(row.PhysicalVolume)}."));
        }

        return new PerformanceReportInsightsDto(items, topNegative, topPositive);
    }

    private static string FormatSigned(decimal? value)
    {
        if (value is null) return "—";
        var v = value.Value;
        return v > 0 ? $"+{v:0.00}" : $"{v:0.00}";
    }

    private static string FormatVolume(decimal? volume) =>
        volume is null ? "—" : $"{volume.Value:0.##}";
}
