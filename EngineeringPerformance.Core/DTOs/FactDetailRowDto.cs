namespace EngineeringPerformance.Core.DTOs;

// Строка таблицы «Детально» вкладки «Оценка факта» страницы «Проекты».
// FactLaborHours — пропорциональная доля часов LI, отнесённая к этой строке PPV
// внутри группы (FacilityCode, WorkDirectionId) по доле объёма (вариант A, ISS-005).
// ZScore — рассчитан по производительности группы (одинаков для всех PPV одной группы).
public sealed record FactDetailRowDto(
    string FacilityCode,
    string FacilityName,
    string BindingType,
    string DocumentSet,
    string CatalogId,
    string Tag,
    string WorkId,
    string WorkName,
    string WorkComment,
    string EstimationBasis,
    string PvCode,
    string PvName,
    string IicGroupName,
    string IicUnitName,
    decimal Quantity,
    decimal? FactLaborHours,
    decimal? FactProductivity,
    decimal? NormProductivity,
    decimal? ZScore,
    string Percentile,
    string Interpretation);
