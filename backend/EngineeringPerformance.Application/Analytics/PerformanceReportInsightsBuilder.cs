using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Выводы и топ дисциплин для вкладки «Отчет».
/// Ранжирование влияния: |Z| × subtreeHours (паритет ТЗ отчёта).
/// </summary>
public static class PerformanceReportInsightsBuilder
{
    public static PerformanceReportInsightsDto Build(
        IReadOnlyList<PerformanceReportDirectionRowDto> factDirections,
        decimal? totalLaborHours,
        decimal? unallocatedLaborHours,
        decimal? accuracyPercent)
    {
        var leaves = factDirections
            .Where(d => !factDirections.Any(c => c.ParentId == d.WorkDirectionId))
            .Where(d => d.ZScore.HasValue)
            .ToList();

        var items = new List<PerformanceReportInsightItemDto>();

        foreach (var row in leaves.Where(r => r.ZScore > 1m).OrderByDescending(r => r.ZScore))
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "risk",
                Title: $"{row.Name}: Z = {FormatSigned(row.ZScore)}",
                Detail: "Перерасход относительно нормы ИИЦ. Требуется разбор причин: состав работ, полнота данных и загрузка исполнителей."));
        }

        foreach (var row in leaves.Where(r => r.ZScore > 0.5m && r.ZScore <= 1m).OrderByDescending(r => r.ZScore))
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "attention",
                Title: $"{row.Name}: Z = {FormatSigned(row.ZScore)}",
                Detail: "Показатель выше нормы, но в пределах одного σ. Поставить направление на контроль до следующей версии факта."));
        }

        var best = leaves.Where(r => r.ZScore < -0.5m).OrderBy(r => r.ZScore).ToList();
        if (best.Count > 0)
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "strength",
                Title: $"Опережение нормы: {string.Join(", ", best.Select(r => r.Name))}",
                Detail: "Практики этих направлений стоит зафиксировать как референс и учесть при актуализации внутренних норм трудоёмкости."));
        }

        var total = totalLaborHours ?? 0m;
        var unallocated = unallocatedLaborHours ?? 0m;
        if (unallocated > 0m)
        {
            var share = total > 0m ? unallocated / total * 100m : 0m;
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "data",
                Title: $"Не отнесено к направлениям: {unallocated:0} ч ({share:0.0}%)",
                Detail: share > 2m
                    ? "Доля нераспределённых часов искажает средневзвешенную Z-оценку. Требуется доразметка строк трудоёмкости."
                    : "Доля нераспределённых часов в допустимых пределах."));
        }

        if (accuracyPercent is < 85m)
        {
            items.Add(new PerformanceReportInsightItemDto(
                Kind: "data",
                Title: $"Точность сравнения: {accuracyPercent:0.0}%",
                Detail: "Часть характеристик проекта слабо представлена в выборке ИИЦ. Итоговую Z-оценку следует трактовать как ориентир."));
        }

        var ranked = leaves
            .Where(r => (r.SubtreeHours ?? 0m) > 0m)
            .Select(r => new PerformanceReportKeyDisciplineDto(
                WorkDirectionId: r.WorkDirectionId,
                Name: r.Name,
                Code: r.Code,
                ZScore: r.ZScore,
                SubtreeHours: r.SubtreeHours ?? 0m,
                InfluenceScore: Math.Abs(r.ZScore ?? 0m) * (r.SubtreeHours ?? 0m)))
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

        return new PerformanceReportInsightsDto(items, topNegative, topPositive);
    }

    private static string FormatSigned(decimal? value)
    {
        if (value is null) return "—";
        var v = value.Value;
        return v > 0 ? $"+{v:0.00}" : $"{v:0.00}";
    }
}
