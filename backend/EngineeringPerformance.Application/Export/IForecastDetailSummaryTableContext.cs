using EngineeringPerformance.Application.Analytics;

namespace EngineeringPerformance.Application.Export;

/// <summary>Метаданные версии и дерево IIC для сводной таблицы прогноза «Детально» и PDF-титулов.</summary>
public sealed record ForecastExportContext(
    string ProjectCode,
    string ProjectName,
    string VersionName,
    DateTime CreatedAt,
    IReadOnlyList<IicDirectionNodeDto> Directions);

/// <summary>Метаданные версии и дерево IIC для сводной таблицы прогноза «Детально».</summary>
public interface IForecastDetailSummaryTableContext
{
    Task<ForecastExportContext> LoadAsync(
        int projectId,
        int versionId,
        CancellationToken cancellationToken = default);
}
