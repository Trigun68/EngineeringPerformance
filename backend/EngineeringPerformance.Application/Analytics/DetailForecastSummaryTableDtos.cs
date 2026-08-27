namespace EngineeringPerformance.Application.Analytics;

/// <summary>Входная строка для построения сводной таблицы «Детально».</summary>
public sealed record DetailForecastSummaryInputRow(
    string FacilityCode,
    string FacilityName,
    int? ClassifierId,
    decimal Quantity,
    decimal? Coefficient,
    /// <summary>Часы выбранного сценария (база / опт. / пессим.).</summary>
    decimal? LaborHours);

/// <summary>Узел дерева групп ФО IIC для определения корневых блоков и ед. изм.</summary>
public sealed record IicDirectionNodeDto(
    int Id,
    int? ParentId,
    string Name,
    string Code,
    int SortOrder,
    string? UnitName = null);

/// <summary>Столбец-блок сводной таблицы (пара ФО + Тр.).</summary>
public sealed record DetailForecastSummaryColumnDto(
    int RootId,
    string RootName,
    string UnitName,
    string BlockTitle);

/// <summary>Значения ячеек блока: физ. объём и трудоёмкость.</summary>
public sealed record DetailForecastSummaryCellDto(
    decimal PhysicalVolume,
    decimal LaborHours);

/// <summary>Строка сводной таблицы — один объект проекта.</summary>
public sealed record DetailForecastSummaryRowDto(
    string FacilityCode,
    string FacilityName,
    string RowLabel,
    IReadOnlyList<DetailForecastSummaryCellDto> Cells,
    /// <summary>Сумма трудоёмкости по всем блокам строки (ФО в итог не входит).</summary>
    decimal LaborHoursTotal);

/// <summary>Сводная таблица прогноза по объектам и корневым группам IIC.</summary>
public sealed record DetailForecastSummaryTableDto(
    string ProjectCode,
    string VersionName,
    IReadOnlyList<DetailForecastSummaryColumnDto> Columns,
    IReadOnlyList<DetailForecastSummaryRowDto> Rows);
