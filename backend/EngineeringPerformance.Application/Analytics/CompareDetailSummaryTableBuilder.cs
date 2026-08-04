namespace EngineeringPerformance.Application.Analytics;

/// <summary>Подготавливает строки единой сводной таблицы детализации сравнения версий.</summary>
public static class CompareDetailSummaryTableBuilder
{
    public static IReadOnlyList<CompareDetailSummaryTableRowDto> Build(
        IReadOnlyList<VersionCompareDetailRowDto> rows) =>
        rows
            .OrderBy(row => row.FacilityCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.IicGroupName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.PvCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.WorkId, StringComparer.OrdinalIgnoreCase)
            .Select(row => new CompareDetailSummaryTableRowDto(
                row.FacilityCode,
                row.FacilityName,
                row.IicGroupName,
                row.PvCode,
                row.PvName,
                row.WorkId,
                row.WorkName,
                row.QuantityA,
                row.QuantityB,
                row.PlannedLaborHoursA,
                row.PlannedLaborHoursB,
                row.FactLaborHoursA,
                row.FactLaborHoursB,
                row.PlannedProductivityA,
                row.PlannedProductivityB,
                row.FactProductivityA,
                row.FactProductivityB,
                row.ZScoreA,
                row.ZScoreB,
                row.Presence))
            .ToList();
}

/// <summary>Строка единой A/B-таблицы без расчёта или агрегации показателей.</summary>
public sealed record CompareDetailSummaryTableRowDto(
    string FacilityCode,
    string FacilityName,
    string IicGroupName,
    string PvCode,
    string PvName,
    string WorkId,
    string WorkName,
    decimal? PhysicalVolumeA,
    decimal? PhysicalVolumeB,
    decimal? PlannedLaborHoursA,
    decimal? PlannedLaborHoursB,
    decimal? FactLaborHoursA,
    decimal? FactLaborHoursB,
    decimal? PlannedProductivityA,
    decimal? PlannedProductivityB,
    decimal? FactProductivityA,
    decimal? FactProductivityB,
    decimal? ZScoreA,
    decimal? ZScoreB,
    string Presence);
