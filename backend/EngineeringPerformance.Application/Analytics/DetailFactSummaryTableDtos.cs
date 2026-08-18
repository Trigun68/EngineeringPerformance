namespace EngineeringPerformance.Application.Analytics;

/// <summary>Входная строка сводной таблицы факта «Детально».</summary>
public sealed record DetailFactSummaryInputRow(
    string FacilityCode,
    string FacilityName,
    int? ClassifierId,
    decimal Quantity,
    decimal? Coefficient,
    decimal? PessimisticHours,
    decimal? BaseHours,
    decimal? OptimisticHours,
    decimal? FactHours,
    decimal? ZScore);

/// <summary>Значения ячеек блока: ФО, три сценария план. часов, факт. часы, Z.</summary>
public sealed record DetailFactSummaryCellDto(
    decimal PhysicalVolume,
    decimal PessimisticHours,
    decimal BaseHours,
    decimal OptimisticHours,
    decimal FactHours,
    decimal? ZScore);

/// <summary>Строка сводной таблицы факта — один объект.</summary>
public sealed record DetailFactSummaryRowDto(
    string FacilityCode,
    string FacilityName,
    string RowLabel,
    IReadOnlyList<DetailFactSummaryCellDto> Cells,
    /// <summary>Сумма факт. трудоёмкости по блокам (ФО не входит).</summary>
    decimal FactLaborHoursTotal);


/// <summary>Сводная таблица факта по объектам и корневым группам IIC.</summary>
public sealed record DetailFactSummaryTableDto(
    string ProjectCode,
    string VersionName,
    IReadOnlyList<DetailForecastSummaryColumnDto> Columns,
    IReadOnlyList<DetailFactSummaryRowDto> Rows);
