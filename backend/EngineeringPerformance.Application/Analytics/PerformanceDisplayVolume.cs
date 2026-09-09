namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Объём для таблиц отчёта и FactNorm. Не дублирует формулу Z — только отображение / ось X точек.
/// На L1 после rollup часов own <see cref="PerformanceRowDto.FactPhysicalVolume"/> часто пуст.
/// </summary>
public static class PerformanceDisplayVolume
{
    public static decimal? ResolveSubtree(PerformanceRowDto row)
    {
        if (row.SubtreePhysicalVolume is > 0)
            return row.SubtreePhysicalVolume;
        if (row.FactLaborHours is > 0 && row.FactProductivity is > 0)
            return row.FactLaborHours.Value / row.FactProductivity.Value;
        return row.FactPhysicalVolume is > 0 ? row.FactPhysicalVolume : null;
    }

    public static decimal? ForReportRow(PerformanceRowDto row) =>
        row.ParentId is null ? ResolveSubtree(row) : row.FactPhysicalVolume;

    public static decimal? ForMergedRow(PerformanceReportMergedDirectionRowDto row) =>
        row.ParentId is null
            ? (row.SubtreePhysicalVolume is > 0 ? row.SubtreePhysicalVolume : row.PhysicalVolume)
            : row.PhysicalVolume;

    public static IEnumerable<PerformanceRowDto> FirstLevel(IEnumerable<PerformanceRowDto> rows) =>
        rows.Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.ParentId is null);

    /// <summary>
    /// Узлы, на которых лежат собственные факт-часы: корни после свёртки на L1
    /// или заявленные узлы исключающей агрегации (не только ParentId == null).
    /// </summary>
    public static IEnumerable<PerformanceRowDto> WithOwnFactHours(IEnumerable<PerformanceRowDto> rows) =>
        rows.Where(r => !r.IsTotal && r.WorkDirectionId >= 0 && r.FactLaborHours is > 0);
}
