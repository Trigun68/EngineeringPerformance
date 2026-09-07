using System.Globalization;
using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Полный табличный Excel сравнения — колонки как в гриде, не сводная детализация.
/// </summary>
internal static class CompareTableDocumentBuilder
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static ForecastTableDocument BuildSummary(
        IReadOnlyList<VersionCompareRowDto> rows,
        string title)
    {
        var columns = new List<ForecastTableColumn>
        {
            new("Направление", 2.6f),
        };
        columns.AddRange(SharedMetricColumns());
        columns.AddRange(PlanZColumns());

        return new ForecastTableDocument(
            title,
            "Итого",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows.Select(MapSummaryRow).ToList())]);
    }

    public static ForecastTableDocument BuildFacility(
        IReadOnlyList<VersionCompareFacilityRowDto> rows,
        string title)
    {
        var columns = new List<ForecastTableColumn>
        {
            new("Код", 1.2f),
            new("Наименование", 2.4f),
        };
        columns.AddRange(SharedMetricColumns());

        return new ForecastTableDocument(
            title,
            "По объектам",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows.Select(MapFacilityRow).ToList())]);
    }

    public static ForecastTableDocument BuildDetail(
        IReadOnlyList<VersionCompareDetailRowDto> rows,
        string title)
    {
        var columns = new List<ForecastTableColumn>
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
            new("Оборудование", 1.0f),
        };
        columns.AddRange(SharedMetricColumns());
        columns.Add(new ForecastTableColumn("Наличие", 1.0f));

        return new ForecastTableDocument(
            title,
            "Детально",
            Landscape: true,
            [new ForecastTableSection(null, columns, rows.Select(MapDetailRow).ToList())]);
    }

    private static ForecastTableColumn[] SharedMetricColumns() =>
    [
        new("ФО (v. А)", 1.1f, Numeric: true),
        new("ФО (v. Б)", 1.1f, Numeric: true),
        new("План.Труд. (Опт. v. А)", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
        new("План.Труд. (Баз. v. А)", 1.3f, Numeric: true, Tone: ForecastHourTone.Base),
        new("План.Труд. (Песс. v. А)", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        new("План.Труд. (Опт. v. Б)", 1.3f, Numeric: true, Tone: ForecastHourTone.Optimistic),
        new("План.Труд. (Баз. v. Б)", 1.3f, Numeric: true, Tone: ForecastHourTone.Base),
        new("План.Труд. (Песс. v. Б)", 1.3f, Numeric: true, Tone: ForecastHourTone.Pessimistic),
        new("Ф.труд. (v. А)", 1.2f, Numeric: true),
        new("Ф.труд. (v. Б)", 1.2f, Numeric: true),
        new("Пл.произв. (v. А)", 1.2f, Numeric: true),
        new("Пл.произв. (v. Б)", 1.2f, Numeric: true),
        new("Ф.произв. (v. А)", 1.2f, Numeric: true),
        new("Ф.произв. (v. Б)", 1.2f, Numeric: true),
        new("Оценка Z (v. А)", 1.1f, Numeric: true),
        new("Оценка Z (v. Б)", 1.1f, Numeric: true),
        new("Δ оценка Z", 1.0f, Numeric: true),
    ];

    private static ForecastTableColumn[] PlanZColumns() =>
    [
        new("Оценка Z плана (v. А)", 1.2f, Numeric: true),
        new("Оценка Z плана (v. Б)", 1.2f, Numeric: true),
        new("Δ оценка Z плана", 1.1f, Numeric: true),
    ];

    private static ForecastTableRow MapSummaryRow(VersionCompareRowDto row)
    {
        var indent = row.Level > 0 ? new string(' ', row.Level * 2) : string.Empty;
        var cells = new List<ForecastTableCell>
        {
            new(indent + row.Name, Bold: row.IsTotal),
        };
        cells.AddRange(SharedMetricCells(
            row.FactPhysicalVolumeDisplayA ?? FormatNumber(row.FactPhysicalVolumeA, 2),
            row.FactPhysicalVolumeA,
            row.FactPhysicalVolumeDisplayB ?? FormatNumber(row.FactPhysicalVolumeB, 2),
            row.FactPhysicalVolumeB,
            row.PlannedLaborHoursOptimisticA,
            row.PlannedLaborHoursA,
            row.PlannedLaborHoursPessimisticA,
            row.PlannedLaborHoursOptimisticB,
            row.PlannedLaborHoursB,
            row.PlannedLaborHoursPessimisticB,
            row.FactLaborHoursA,
            row.FactLaborHoursB,
            row.PlannedProductivityA,
            row.PlannedProductivityB,
            row.FactProductivityA,
            row.FactProductivityB,
            row.FactZScoreA,
            row.FactZScoreB,
            row.FactZDelta));
        cells.Add(Number(row.PlanZScoreA, 4));
        cells.Add(Number(row.PlanZScoreB, 4));
        cells.Add(Number(row.PlanZDelta, 4));
        return new ForecastTableRow(cells, IsTotal: row.IsTotal);
    }

    private static ForecastTableRow MapFacilityRow(VersionCompareFacilityRowDto row)
    {
        var cells = new List<ForecastTableCell>
        {
            Text(row.FacilityCode),
            Text(row.FacilityName),
        };
        cells.AddRange(SharedMetricCells(
            FormatNumber(row.FactPhysicalVolumeA, 2),
            row.FactPhysicalVolumeA,
            FormatNumber(row.FactPhysicalVolumeB, 2),
            row.FactPhysicalVolumeB,
            row.PlannedLaborHoursOptimisticA,
            row.PlannedLaborHoursA,
            row.PlannedLaborHoursPessimisticA,
            row.PlannedLaborHoursOptimisticB,
            row.PlannedLaborHoursB,
            row.PlannedLaborHoursPessimisticB,
            row.FactLaborHoursA,
            row.FactLaborHoursB,
            row.PlannedProductivityA,
            row.PlannedProductivityB,
            row.FactProductivityA,
            row.FactProductivityB,
            row.ZScoreA,
            row.ZScoreB,
            row.ZDelta));
        return new ForecastTableRow(cells);
    }

    private static ForecastTableRow MapDetailRow(VersionCompareDetailRowDto row)
    {
        var itemName = string.IsNullOrWhiteSpace(row.ItemName) ? row.WorkName : row.ItemName;
        var cells = new List<ForecastTableCell>
        {
            Text(row.FacilityCode),
            Text(row.FacilityName),
            Text(row.BindingType),
            Text(row.DocumentSet),
            Text(row.CatalogId),
            Text(row.Tag),
            Text(row.WorkId),
            Text(itemName),
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
            Text(row.IsEquipment ? "Да" : "Нет"),
        };
        cells.AddRange(SharedMetricCells(
            FormatNumber(row.QuantityA, 4),
            row.QuantityA,
            FormatNumber(row.QuantityB, 4),
            row.QuantityB,
            row.PlannedLaborHoursOptimisticA,
            row.PlannedLaborHoursA,
            row.PlannedLaborHoursPessimisticA,
            row.PlannedLaborHoursOptimisticB,
            row.PlannedLaborHoursB,
            row.PlannedLaborHoursPessimisticB,
            row.FactLaborHoursA,
            row.FactLaborHoursB,
            row.PlannedProductivityA,
            row.PlannedProductivityB,
            row.FactProductivityA,
            row.FactProductivityB,
            row.ZScoreA,
            row.ZScoreB,
            row.ZDelta));
        cells.Add(Text(row.Presence));
        return new ForecastTableRow(cells);
    }

    private static IReadOnlyList<ForecastTableCell> SharedMetricCells(
        string? foAText,
        decimal? foA,
        string? foBText,
        decimal? foB,
        decimal? optA,
        decimal? baseA,
        decimal? pesA,
        decimal? optB,
        decimal? baseB,
        decimal? pesB,
        decimal? factA,
        decimal? factB,
        decimal? planProdA,
        decimal? planProdB,
        decimal? factProdA,
        decimal? factProdB,
        decimal? zA,
        decimal? zB,
        decimal? zDelta) =>
    [
        new ForecastTableCell(foAText ?? "—", foA),
        new ForecastTableCell(foBText ?? "—", foB),
        Hour(optA, ForecastHourTone.Optimistic, 1),
        Hour(baseA, ForecastHourTone.Base, 1),
        Hour(pesA, ForecastHourTone.Pessimistic, 1),
        Hour(optB, ForecastHourTone.Optimistic, 1),
        Hour(baseB, ForecastHourTone.Base, 1),
        Hour(pesB, ForecastHourTone.Pessimistic, 1),
        Hour(factA, ForecastHourTone.None, 1),
        Hour(factB, ForecastHourTone.None, 1),
        Number(planProdA, 4),
        Number(planProdB, 4),
        Number(factProdA, 4),
        Number(factProdB, 4),
        Number(zA, 4),
        Number(zB, 4),
        Number(zDelta, 4),
    ];

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
