namespace EngineeringPerformance.Core.DTOs;

/// <summary>
/// Строка дерева групп физ. объёмов (ИИС) для прогноза: нормы на дату версии анализа, плановый объём и расчёт трудозатрат по сценариям.
/// </summary>
public sealed record ForecastIicVolumeRowDto(
    int WorkDirectionId,
    int? ParentId,
    string Name,
    string? UnitName,
    decimal? NormProductivity,
    decimal? NormEstimate,
    decimal? NormStandardDeviation,
    decimal PlannedVolume,
    decimal OptimisticHours,
    decimal BaseHours,
    decimal PessimisticHours,
    string Range,
    /// <summary>Полный путь «родитель / … / узел» для плоских таблиц сценариев.</summary>
    string HierarchyPath);
