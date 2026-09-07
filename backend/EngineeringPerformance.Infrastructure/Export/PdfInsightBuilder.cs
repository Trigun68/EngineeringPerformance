using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Краткие авто-выводы (callout-блоки) для PDF — порт логики
/// client/src/pages/projects/reports/reportCalc.ts::buildInsights, сжатый до
/// максимум 3 блоков на отчёт (риск → внимание → сильная сторона → данные).
/// </summary>
public static class PdfInsightBuilder
{
    private const int MaxCalloutsPerReport = 3;

    /// <summary>Отчёт «Оценка факта» — полноценные выводы на основе Z-score и полноты данных.</summary>
    public static IReadOnlyList<PdfCallout> BuildFactCallouts(
        IReadOnlyList<PerformanceRowDto> rows,
        ProjectVersionAnalysisSummaryDto summary)
    {
        var scored = PerformanceDisplayVolume.FirstLevel(rows)
            .Where(r => r.ZScore.HasValue)
            .OrderByDescending(r => r.ZScore!.Value)
            .ToList();

        var result = new List<PdfCallout>();

        var riskRows = scored.Where(r => r.ZScore!.Value > 1m).ToList();
        if (riskRows.Count > 0)
        {
            var worst = riskRows[0];
            var overrun = ComputeOverrun(worst);
            var extra = riskRows.Count > 1 ? $" Всего направлений в зоне риска (Z > 1): {riskRows.Count}." : string.Empty;
            result.Add(new PdfCallout(
                PdfCalloutKind.Risk,
                $"{worst.WorkDirectionName}: Z = {FormatSigned(worst.ZScore)}",
                $"Наибольший перерасход относительно нормы IIC{(overrun is null ? "" : $" — {PdfNumberFormat.HoursWithUnit(overrun)}")}.{extra} Требуется разбор причин: пересмотреть состав работ и проверить полноту исходных данных."));
        }

        var attentionRows = scored.Where(r => r.ZScore!.Value > 0.5m && r.ZScore!.Value <= 1m).ToList();
        if (attentionRows.Count > 0)
        {
            var worst = attentionRows[0];
            var extra = attentionRows.Count > 1 ? $" Ещё {attentionRows.Count - 1} направлени(е/й) в этой зоне." : string.Empty;
            result.Add(new PdfCallout(
                PdfCalloutKind.Attention,
                $"{worst.WorkDirectionName}: Z = {FormatSigned(worst.ZScore)}",
                $"Показатель выше нормы, но в пределах одного σ.{extra} Поставить на контроль до следующей версии факта."));
        }

        var strongRows = scored.Where(r => r.ZScore!.Value < -0.5m).Reverse().ToList();
        if (strongRows.Count > 0)
        {
            var names = strongRows.Take(3).Select(r => r.WorkDirectionName).ToList();
            var namesLabel = string.Join(", ", names) + (strongRows.Count > 3 ? " и др." : string.Empty);
            result.Add(new PdfCallout(
                PdfCalloutKind.Strength,
                $"Опережение нормы: {namesLabel}",
                "Практики этих направлений стоит зафиксировать как референс и учесть при актуализации внутренних норм трудоёмкости."));
        }

        var totalFactLaborHours = summary.TotalFactLaborHours ?? 0m;
        var unallocated = summary.UnallocatedLaborHours ?? 0m;
        if (unallocated > 0m)
        {
            var share = totalFactLaborHours > 0m ? unallocated / totalFactLaborHours * 100m : 0m;
            result.Add(new PdfCallout(
                PdfCalloutKind.Data,
                $"Не отнесено к направлениям: {unallocated:N0} ч ({share:N1}%)",
                share > 2m
                    ? "Доля нераспределённых часов искажает средневзвешенную Z-оценку. Требуется доразметка строк трудоёмкости при следующем импорте."
                    : "Доля нераспределённых часов в допустимых пределах и существенно не влияет на итоговую Z-оценку."));
        }

        return OrderAndCap(result);
    }

    /// <summary>Прогноз — одна авто-фраза о разбросе сценариев.</summary>
    public static PdfCallout? BuildForecastCallout(decimal baseHours, decimal optimisticHours, decimal pessimisticHours)
    {
        var spread = pessimisticHours - optimisticHours;
        if (baseHours <= 0m && spread == 0m) return null;
        var sharePct = baseHours > 0m ? spread / baseHours * 100m : 0m;
        return new PdfCallout(
            PdfCalloutKind.Data,
            $"Разброс сценариев: {PdfNumberFormat.HoursWithUnit(spread)} ({sharePct.ToString("N1", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"))}% от базового)",
            $"Диапазон между оптимистичным и пессимистичным сценарием составляет {PdfNumberFormat.HoursWithUnit(spread)} при базовом объёме {PdfNumberFormat.HoursWithUnit(baseHours)}. Используйте пессимистичный сценарий как верхнюю границу риска при планировании ресурсов.");
    }

    /// <summary>Сравнение версий — одна авто-фраза о наибольшем отклонении.</summary>
    public static PdfCallout? BuildCompareCallout(
        IEnumerable<(string Name, decimal? Delta)> deltas,
        string versionAName,
        string versionBName)
    {
        var top = deltas
            .Where(d => d.Delta.HasValue)
            .OrderByDescending(d => Math.Abs(d.Delta!.Value))
            .FirstOrDefault();
        if (top.Delta is null || top.Delta.Value == 0m) return null;

        var direction = top.Delta.Value > 0 ? "увеличение" : "снижение";
        return new PdfCallout(
            top.Delta.Value > 0 ? PdfCalloutKind.Risk : PdfCalloutKind.Strength,
            $"Наибольшее отклонение: {top.Name}",
            $"Между версиями «{versionAName}» и «{versionBName}» зафиксировано наибольшее {direction} — {Math.Abs(top.Delta.Value):N2} по направлению «{top.Name}».");
    }

    /// <summary>Тренды — одна авто-фраза о наибольшем отклонении факта от прогноза по точкам ряда.</summary>
    public static PdfCallout? BuildTrendCallout(IEnumerable<(string? Label, DateTime Timestamp, decimal? Delta)> deltaPoints)
    {
        var top = deltaPoints
            .Where(p => p.Delta.HasValue)
            .OrderByDescending(p => Math.Abs(p.Delta!.Value))
            .FirstOrDefault();
        if (top.Delta is null || top.Delta.Value == 0m) return null;

        var label = !string.IsNullOrWhiteSpace(top.Label) ? top.Label : top.Timestamp.ToString("dd.MM.yyyy");
        var direction = top.Delta.Value > 0 ? "превысил прогноз" : "отстал от прогноза";
        return new PdfCallout(
            top.Delta.Value > 0 ? PdfCalloutKind.Attention : PdfCalloutKind.Data,
            $"Наибольшее отклонение — {label}",
            $"Факт {direction} на {Math.Abs(top.Delta.Value):N0} ч. Проверить причины расхождения перед актуализацией следующей версии прогноза.");
    }

    private static decimal? ComputeOverrun(PerformanceRowDto row)
    {
        var volume = PerformanceDisplayVolume.ResolveSubtree(row);
        return row.FactLaborHours.HasValue && row.NormProductivity.HasValue && volume.HasValue
            ? row.FactLaborHours.Value - row.NormProductivity.Value * volume.Value
            : null;
    }

    private static string FormatSigned(decimal? value) =>
        value is null ? "—" : (value.Value > 0 ? "+" : string.Empty) + value.Value.ToString("N2");

    private static IReadOnlyList<PdfCallout> OrderAndCap(List<PdfCallout> callouts)
    {
        var priority = new Dictionary<PdfCalloutKind, int>
        {
            [PdfCalloutKind.Risk] = 0,
            [PdfCalloutKind.Attention] = 1,
            [PdfCalloutKind.Strength] = 2,
            [PdfCalloutKind.Data] = 3,
        };
        return callouts
            .OrderBy(c => priority[c.Kind])
            .Take(MaxCalloutsPerReport)
            .ToList();
    }
}
