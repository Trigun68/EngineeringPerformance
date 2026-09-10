using System.Globalization;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Таблицы экспорта факта. Математики нет — только раскладка уже посчитанных метрик.
/// Excel и PDF рендерятся через <see cref="ForecastTableRenderers"/>.
/// </summary>
internal static class FactTableDocumentBuilder
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private const int FacilityTopCount = 10;

    public static ForecastTableDocument BuildAnalysis(ProjectVersionAnalysisDto report)
    {
        var columns = AnalysisColumns();
        var rows = new List<ForecastTableRow> { MapAnalysisTotalRow(report) };
        foreach (var direction in report.Directions.Where(d => d.Id >= 0))
            rows.Add(MapAnalysisDirectionRow(direction));

        return new ForecastTableDocument(
            "Анализ версии",
            "Анализ",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows)]);
    }

    public static ForecastTableDocument BuildSummary(IReadOnlyList<PerformanceRowDto> rows, string title)
    {
        var columns = new ForecastTableColumn[]
        {
            new("Код", 1.1f),
            new("Направление", 2.4f),
            new("Ед. изм.", 0.9f),
            new("Физобъём", 1.4f, Numeric: true),
            new("Сгруппированный физобъём", 1.5f, Numeric: true),
            new("Факт трудоёмк.", 1.3f, Numeric: true),
            new("План.Труд.Оптим.", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
            new("План.Труд.Баз.", 1.2f, Numeric: true, Tone: ForecastHourTone.Base),
            new("План.Труд.Песс.", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
            new("Факт производит.", 1.3f, Numeric: true),
            new("Норм производит.", 1.3f, Numeric: true),
            new("Оценка производ.", 1.2f, Numeric: true),
            new("Интерпретация", 1.8f),
        };

        return new ForecastTableDocument(
            title,
            "Итого",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows.Select(MapSummaryRow).ToList())]);
    }

    public static ForecastTableDocument BuildFacility(IReadOnlyList<ProjectsPageFacilityRowDto> rows, string title)
    {
        var columns = FacilityColumns();
        var sections = new List<ForecastTableSection>();
        if (rows.Count > 0)
        {
            var top = rows
                .OrderByDescending(r => r.LaborHours ?? 0m)
                .Take(FacilityTopCount)
                .Select(MapFacilityRow)
                .ToList();
            sections.Add(new ForecastTableSection($"Топ-{FacilityTopCount} объектов", columns, top));
        }

        var all = rows.Select(MapFacilityRow).ToList();
        all.Add(FacilityTotalRow(rows));
        sections.Add(new ForecastTableSection("Все объекты", columns, all));

        return new ForecastTableDocument(title, "По объектам", Landscape: true, sections);
    }

    public static ForecastTableDocument BuildDetail(IReadOnlyList<ProjectsPageDetailRowDto> rows, string title)
    {
        var columns = new ForecastTableColumn[]
        {
            new("Шифр объекта", 1.2f),
            new("Наименование объекта", 2.0f),
            new("Тип привязки", 1.1f),
            new("Комплект документов", 1.4f),
            new("ID по каталогу", 1.1f),
            new("TAG", 0.9f),
            new("ID работы", 1.0f),
            new("Наименование", 1.8f),
            new("Комментарий", 1.4f),
            new("Описательная часть", 1.4f),
            new("Код", 0.9f),
            new("Наименование ФО", 1.6f),
            new("Оценка", 0.8f),
            new("Коэфф.", 0.8f, Numeric: true),
            new("Группа ФО IIC", 1.8f),
            new("Ед. изм.", 0.8f),
            new("Производит.", 1.1f, Numeric: true),
            new("СО", 0.8f, Numeric: true),
            new("Кол-во", 0.9f, Numeric: true),
            new("Факт.трудоёмк., ч", 1.1f, Numeric: true),
            new("План.Труд.Оптим.", 1.2f, Numeric: true, Tone: ForecastHourTone.Optimistic),
            new("План.Труд.Баз.", 1.1f, Numeric: true, Tone: ForecastHourTone.Base),
            new("План.Труд.Песс.", 1.2f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
            new("Факт производит.", 1.1f, Numeric: true),
            new("Оценка Z", 0.9f, Numeric: true),
            new("Интерпретация", 1.6f),
        };

        return new ForecastTableDocument(
            title,
            "Детально",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows.Select(MapDetailRow).Append(DetailTotalRow(rows)).ToList())]);
    }

    private static ForecastTableColumn[] AnalysisColumns() =>
    [
        new("Шифр", 1.0f),
        new("Направление", 2.6f),
        new("Физобъём", 1.2f, Numeric: true),
        new("Сгруппированный физобъём", 1.4f, Numeric: true),
        new("Факт ч", 1.1f, Numeric: true),
        new("План.Труд.Оптим.", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
        new("План.Труд.Баз.", 1.2f, Numeric: true, Tone: ForecastHourTone.Base),
        new("План.Труд.Песс.", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        new("Факт произв.", 1.2f, Numeric: true),
        new("Норм. произв.", 1.2f, Numeric: true),
        new("Z", 0.9f, Numeric: true),
        new("Интерпретация", 1.8f),
    ];

    private static ForecastTableColumn[] FacilityColumns() =>
    [
        new("Шифр объекта", 1.2f),
        new("Наименование объекта", 3.4f),
        new("Факт.трудоёмк., ч", 1.3f, Numeric: true),
        new("План.Труд.Оптим.", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
        new("План.Труд.Баз.", 1.2f, Numeric: true, Tone: ForecastHourTone.Base),
        new("План.Труд.Песс.", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        new("Оценка Z", 1.1f, Numeric: true),
        new("Интерпретация", 1.8f),
    ];

    private static ForecastTableRow MapAnalysisDirectionRow(ProjectVersionDirectionDto d)
    {
        var indent = d.Level > 0 ? new string(' ', d.Level * 2) : string.Empty;
        return new ForecastTableRow(
        [
            Text(d.Code),
            new ForecastTableCell(indent + d.Name),
            Number(d.PhysicalVolume, 2),
            Number(d.AssessmentPhysicalVolume, 2),
            Hour(d.FactLaborHours, ForecastHourTone.None, 1),
            Hour(d.OptimisticHours, ForecastHourTone.Optimistic, 1),
            Hour(d.BaseHours, ForecastHourTone.Base, 1),
            Hour(d.PessimisticHours, ForecastHourTone.Pessimistic, 1),
            Number(d.FactProductivity, 4),
            Number(d.NormProductivity, 4),
            Number(d.ZScore, 4),
            Text(d.Interpretation.ToRussian()),
        ]);
    }

    private static ForecastTableRow MapAnalysisTotalRow(ProjectVersionAnalysisDto report)
    {
        var all = report.Directions.Where(d => d.Id >= 0).ToList();
        var fo = string.Join(
            "; ",
            report.Summary.PhysicalVolumeTotalsByUnit
                .Select(u => $"{FormatNumber(u.TotalVolume, 2)} {u.UnitName}".Trim()));
        if (string.IsNullOrWhiteSpace(fo))
            fo = "—";

        return new ForecastTableRow(
        [
            new ForecastTableCell("По проекту", Bold: true),
            new ForecastTableCell(string.Empty, Bold: true),
            new ForecastTableCell(fo, Bold: true),
            new ForecastTableCell("—", Bold: true),
            Hour(report.Summary.TotalFactLaborHours, ForecastHourTone.None, 1),
            Hour(all.Sum(d => d.OptimisticHours ?? 0m), ForecastHourTone.Optimistic, 1),
            Hour(all.Sum(d => d.BaseHours ?? 0m), ForecastHourTone.Base, 1),
            Hour(all.Sum(d => d.PessimisticHours ?? 0m), ForecastHourTone.Pessimistic, 1),
            new ForecastTableCell("—", Bold: true),
            new ForecastTableCell("—", Bold: true),
            Number(report.Summary.WeightedZScore, 4),
            Text(report.Summary.WeightedZScoreInterpretation.ToRussian()),
        ], IsTotal: true);
    }

    private static ForecastTableRow MapSummaryRow(PerformanceRowDto row)
    {
        var indent = row.Level > 0 ? new string(' ', row.Level * 2) : string.Empty;
        var physical = !string.IsNullOrWhiteSpace(row.PhysicalVolumeDisplay)
            ? row.PhysicalVolumeDisplay
            : FormatNumber(row.PhysicalVolume, 2);
        var assessment = !string.IsNullOrWhiteSpace(row.AssessmentPhysicalVolumeDisplay)
            ? row.AssessmentPhysicalVolumeDisplay
            : FormatNumber(row.AssessmentPhysicalVolume, 2);
        return new ForecastTableRow(
        [
            Text(row.WorkDirectionCode),
            new ForecastTableCell(indent + row.WorkDirectionName, Bold: row.IsTotal),
            new ForecastTableCell(row.NormUnitName ?? string.Empty, Bold: row.IsTotal),
            new ForecastTableCell(physical ?? "—", row.PhysicalVolume, Bold: row.IsTotal),
            new ForecastTableCell(assessment ?? "—", row.AssessmentPhysicalVolume, Bold: row.IsTotal),
            Hour(row.FactLaborHours, ForecastHourTone.None, 1),
            Hour(row.OptimisticHours, ForecastHourTone.Optimistic, 1),
            Hour(row.BaseHours, ForecastHourTone.Base, 1),
            Hour(row.PessimisticHours, ForecastHourTone.Pessimistic, 1),
            Number(row.FactProductivity, 4),
            Number(row.NormProductivity, 4),
            Number(row.ZScore, 4),
            Text(row.Interpretation.ToRussian()),
        ], IsTotal: row.IsTotal);
    }

    private static ForecastTableRow MapFacilityRow(ProjectsPageFacilityRowDto row) =>
        new(
        [
            Text(row.FacilityCode),
            Text(row.FacilityName),
            Hour(row.LaborHours, ForecastHourTone.None, 1),
            Hour(row.OptimisticHours, ForecastHourTone.Optimistic, 1),
            Hour(row.BaseHours, ForecastHourTone.Base, 1),
            Hour(row.PessimisticHours, ForecastHourTone.Pessimistic, 1),
            Number(row.ZScore, 4),
            Text(row.Interpretation),
        ]);

    private static ForecastTableRow FacilityTotalRow(IReadOnlyList<ProjectsPageFacilityRowDto> rows)
    {
        decimal fact = 0m, opt = 0m, bas = 0m, pes = 0m;
        foreach (var row in rows)
        {
            fact += row.LaborHours ?? 0m;
            opt += row.OptimisticHours ?? 0m;
            bas += row.BaseHours ?? 0m;
            pes += row.PessimisticHours ?? 0m;
        }

        return new ForecastTableRow(
        [
            new ForecastTableCell("ИТОГО", Bold: true),
            new ForecastTableCell(string.Empty, Bold: true),
            Hour(fact, ForecastHourTone.None, 1),
            Hour(opt, ForecastHourTone.Optimistic, 1),
            Hour(bas, ForecastHourTone.Base, 1),
            Hour(pes, ForecastHourTone.Pessimistic, 1),
            new ForecastTableCell(string.Empty, Bold: true),
            new ForecastTableCell(string.Empty, Bold: true),
        ], IsTotal: true);
    }

    private static ForecastTableRow MapDetailRow(ProjectsPageDetailRowDto row) =>
        new(
        [
            Text(row.FacilityCode),
            Text(row.FacilityName),
            Text(row.BindingType),
            Text(row.DocumentSet),
            Text(row.CatalogId),
            Text(row.Tag),
            Text(row.WorkId),
            Text(row.ItemName),
            Text(row.WorkComment),
            Text(row.EstimationBasis),
            Text(row.PvCode),
            Text(row.PvName),
            Text(row.IsAssessment),
            DecimalCell(row.Coefficient),
            Text(row.IicGroupName),
            Text(row.IicUnitName),
            DecimalCell(row.NormProductivity),
            DecimalCell(row.NormStdDev),
            DecimalCell(row.Quantity),
            DecimalCell(row.FactLaborHours),
            DecimalCell(row.OptimisticHours, ForecastHourTone.Optimistic),
            DecimalCell(row.BaseHours, ForecastHourTone.Base),
            DecimalCell(row.PessimisticHours, ForecastHourTone.Pessimistic),
            DecimalCell(row.FactProductivity),
            DecimalCell(row.ZScore),
            Text(row.Interpretation),
        ]);

    private static ForecastTableRow DetailTotalRow(IReadOnlyList<ProjectsPageDetailRowDto> rows)
    {
        decimal fact = 0m, opt = 0m, bas = 0m, pes = 0m;
        foreach (var row in rows)
        {
            fact += row.FactLaborHours ?? 0m;
            opt += row.OptimisticHours ?? 0m;
            bas += row.BaseHours ?? 0m;
            pes += row.PessimisticHours ?? 0m;
        }

        var cells = new ForecastTableCell[26];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = new ForecastTableCell(string.Empty, Bold: true);
        cells[0] = new ForecastTableCell("ИТОГО", Bold: true);
        cells[19] = DecimalCell(fact);
        cells[20] = DecimalCell(opt, ForecastHourTone.Optimistic);
        cells[21] = DecimalCell(bas, ForecastHourTone.Base);
        cells[22] = DecimalCell(pes, ForecastHourTone.Pessimistic);
        return new ForecastTableRow(cells, IsTotal: true);
    }

    private static ForecastTableCell Text(string? value) =>
        new(value ?? string.Empty);

    private static ForecastTableCell Number(decimal? value, int decimals) =>
        value.HasValue
            ? new ForecastTableCell(FormatNumber(value, decimals), value)
            : new ForecastTableCell("—");

    private static ForecastTableCell Hour(decimal? value, ForecastHourTone tone, int decimals)
    {
        if (!value.HasValue)
            return new ForecastTableCell("—", Tone: tone);

        var rounded = Math.Round(value.Value, decimals, MidpointRounding.AwayFromZero);
        return new ForecastTableCell(FormatNumber(rounded, decimals), rounded, tone);
    }

    private static ForecastTableCell DecimalCell(decimal? value, ForecastHourTone tone = ForecastHourTone.None) =>
        value.HasValue
            ? new ForecastTableCell(value.Value.ToString("G29", Ru), value, tone)
            : new ForecastTableCell(string.Empty, Tone: tone);

    private static string FormatNumber(decimal? value, int decimals)
    {
        if (!value.HasValue)
            return "—";
        var format = decimals <= 0 ? "N0" : $"N{decimals}";
        return value.Value.ToString(format, Ru);
    }
}
