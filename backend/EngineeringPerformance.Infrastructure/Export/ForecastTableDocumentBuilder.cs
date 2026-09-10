using System.Globalization;
using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Собирает строки/колонки экспорта прогноза. Математики нет — только отображение уже посчитанных часов.
/// </summary>
internal static class ForecastTableDocumentBuilder
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private const int FacilityTopCount = 10;

    public static ForecastTableDocument BuildSummary(
        IReadOnlyList<ProjectsPageForecastSummaryRowDto> rows,
        string title)
    {
        var columns = new ForecastTableColumn[]
        {
            new("Шифр", 1.1f),
            new("Направление", 3.2f),
            new("Физобъём", 1.3f, Numeric: true),
            new("Норма производительности", 1.4f, Numeric: true),
            new("Оптимистический, ч", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
            new("Базовый, ч", 1.2f, Numeric: true, Tone: ForecastHourTone.Base),
            new("Пессимистический, ч", 1.4f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        };

        var tableRows = rows.Select(MapSummaryRow).ToList();
        return new ForecastTableDocument(
            title,
            "Итого",
            Landscape: true,
            [new ForecastTableSection(null, columns, tableRows)]);
    }

    public static ForecastTableDocument BuildFacility(
        IReadOnlyList<ProjectsPageFacilityRowDto> rows,
        string title)
    {
        var columns = FacilityColumns();
        var ranked = rows
            .Select(r => (Row: r, Base: r.BaseHours ?? r.LaborHours ?? 0m))
            .OrderByDescending(x => x.Base)
            .Take(FacilityTopCount)
            .Select(x => MapFacilityRow(x.Row))
            .ToList();

        var all = rows.Select(MapFacilityRow).ToList();
        all.Add(FacilityTotalRow(rows));

        return new ForecastTableDocument(
            title,
            "По объектам",
            Landscape: true,
            [
                new ForecastTableSection($"Топ-{FacilityTopCount} объектов", columns, ranked),
                new ForecastTableSection("Все объекты", columns, all),
            ]);
    }

    public static ForecastTableDocument BuildDetail(
        IReadOnlyList<ProjectsPageDetailRowDto> rows,
        string title)
    {
        var columns = new ForecastTableColumn[]
        {
            new("Шифр объекта", 1.2f),
            new("Наименование объекта", 2.2f),
            new("Тип привязки", 1.1f),
            new("Комплект документов", 1.4f),
            new("ID по каталогу", 1.1f),
            new("TAG", 1f),
            new("ID работы", 1f),
            new("Наименование", 1.8f),
            new("Комментарий", 1.4f),
            new("Описательная часть", 1.4f),
            new("Код", 1f),
            new("Наименование ФО", 1.6f),
            new("Оценка", 0.8f),
            new("Коэфф.", 0.8f, Numeric: true),
            new("Группа ФО IIC", 1.8f),
            new("Ед. изм.", 0.8f),
            new("Производит.", 1.1f, Numeric: true),
            new("СО", 0.8f, Numeric: true),
            new("Кол-во", 0.9f, Numeric: true),
            new("Оптимистический", 1.2f, Numeric: true, Tone: ForecastHourTone.Optimistic),
            new("Базовый", 1.1f, Numeric: true, Tone: ForecastHourTone.Base),
            new("Пессимистический", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        };

        var tableRows = rows.Select(MapDetailRow).ToList();
        tableRows.Add(DetailTotalRow(rows));
        return new ForecastTableDocument(
            title,
            "Детально",
            Landscape: true,
            [new ForecastTableSection(null, columns, tableRows)]);
    }

    private static ForecastTableColumn[] FacilityColumns() =>
    [
        new("Шифр объекта", 1.2f),
        new("Наименование объекта", 4.2f),
        new("Оптимистический", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
        new("Базовый", 1.2f, Numeric: true, Tone: ForecastHourTone.Base),
        new("Пессимистический", 1.4f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
    ];

    private static ForecastTableRow MapSummaryRow(ProjectsPageForecastSummaryRowDto row)
    {
        var indent = row.Level > 0 ? new string(' ', row.Level * 2) : string.Empty;
        // Own физобъём; exclusive remap FactPhysicalVolume в прогнозе не показываем.
        var factText = !string.IsNullOrWhiteSpace(row.PhysicalVolumeDisplay)
            ? row.PhysicalVolumeDisplay
            : FormatNumber(row.PhysicalVolume, 2);
        return new ForecastTableRow(
            [
                Text(row.Code),
                new ForecastTableCell(indent + row.DisplayName, Bold: row.IsTotal),
                new ForecastTableCell(factText, row.PhysicalVolume),
                Number(row.NormProductivity, 4),
                Hour(row.OptimisticHours, ForecastHourTone.Optimistic, 1),
                Hour(row.BaseHours, ForecastHourTone.Base, 1),
                Hour(row.PessimisticHours, ForecastHourTone.Pessimistic, 1),
            ],
            IsTotal: row.IsTotal);
    }

    private static ForecastTableRow MapFacilityRow(ProjectsPageFacilityRowDto row)
    {
        var baseH = row.BaseHours ?? row.LaborHours;
        return new ForecastTableRow(
            [
                Text(row.FacilityCode),
                Text(row.FacilityName),
                Hour(row.OptimisticHours, ForecastHourTone.Optimistic, 0),
                Hour(baseH, ForecastHourTone.Base, 0),
                Hour(row.PessimisticHours, ForecastHourTone.Pessimistic, 0),
            ]);
    }

    private static ForecastTableRow FacilityTotalRow(IReadOnlyList<ProjectsPageFacilityRowDto> rows)
    {
        decimal totalBase = 0m, totalOpt = 0m, totalPes = 0m;
        foreach (var row in rows)
        {
            totalBase += row.BaseHours ?? row.LaborHours ?? 0m;
            totalOpt += row.OptimisticHours ?? 0m;
            totalPes += row.PessimisticHours ?? 0m;
        }

        return new ForecastTableRow(
            [
                new ForecastTableCell("ИТОГО", Bold: true),
                new ForecastTableCell(string.Empty, Bold: true),
                Hour(totalOpt, ForecastHourTone.Optimistic, 0),
                Hour(totalBase, ForecastHourTone.Base, 0),
                Hour(totalPes, ForecastHourTone.Pessimistic, 0),
            ],
            IsTotal: true);
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
            DecimalCell(row.OptimisticHours, ForecastHourTone.Optimistic),
            DecimalCell(row.BaseHours, ForecastHourTone.Base),
            DecimalCell(row.PessimisticHours, ForecastHourTone.Pessimistic),
        ]);

    private static ForecastTableRow DetailTotalRow(IReadOnlyList<ProjectsPageDetailRowDto> rows)
    {
        decimal totalOpt = 0m, totalBase = 0m, totalPes = 0m;
        foreach (var row in rows)
        {
            totalOpt += row.OptimisticHours ?? 0m;
            totalBase += row.BaseHours ?? 0m;
            totalPes += row.PessimisticHours ?? 0m;
        }

        var cells = new ForecastTableCell[22];
        for (var i = 0; i < cells.Length; i++)
            cells[i] = new ForecastTableCell(string.Empty, Bold: true);
        cells[0] = new ForecastTableCell("ИТОГО", Bold: true);
        cells[19] = DecimalCell(totalOpt, ForecastHourTone.Optimistic);
        cells[20] = DecimalCell(totalBase, ForecastHourTone.Base);
        cells[21] = DecimalCell(totalPes, ForecastHourTone.Pessimistic);
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
