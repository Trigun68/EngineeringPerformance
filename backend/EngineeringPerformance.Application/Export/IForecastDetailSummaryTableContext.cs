using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Export;

/// <summary>Метаданные версии и дерево IIC для сводной таблицы прогноза «Детально».</summary>
public interface IForecastDetailSummaryTableContext
{
    Task<(string ProjectCode, string ProjectName, string VersionName, IReadOnlyList<IicDirectionNodeDto> Directions)> LoadAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}
