using System.Collections.Generic;
using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectForecastService
{
    /// <summary>
    /// Прогноз по проекту. Если задан <paramref name="nodePlannedTotalsByWorkDirectionId"/>,
    /// значения трактуются как плановый объём по каждому узлу дерева ИИС (как в таблице на форме);
    /// из них восстанавливаются «прямые» объёмы для расчёта трудозатрат.
    /// </summary>
    Task<ProjectForecastReportDto> GenerateForecastAsync(
        int projectId,
        int versionId,
        IReadOnlyDictionary<int, decimal>? nodePlannedTotalsByWorkDirectionId = null);

    /// <summary>
    /// Физ. объёмы и оборудование оценки, сгруппированные по ClassifierId (направление ИИС).
    /// </summary>
    Task<ProjectForecastAssessmentVolumesDto> GetAssessmentVolumesByDirectionAsync(
        int projectId,
        int versionId);
}
