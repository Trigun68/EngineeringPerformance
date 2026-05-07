using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectForecastExportService
{
    Task ExportToExcelAsync(ProjectVersionAnalysisDto report, string filePath);
    Task ExportToPdfAsync(ProjectVersionAnalysisDto report, string filePath);
    Task ExportForecastSummaryToExcelAsync(IReadOnlyList<ForecastSummaryRowDto> rows, string projectCode, string versionName, string filePath);
    Task ExportFacilityForecastToExcelAsync(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath);
    Task ExportFacilityForecastToPdfAsync(IReadOnlyList<FacilityForecastRowDto> rows, string projectCode, string versionName, string filePath);
    Task ExportDetailForecastToExcelAsync(IReadOnlyList<DetailForecastRowDto> rows, string projectCode, string versionName, string filePath);
}
