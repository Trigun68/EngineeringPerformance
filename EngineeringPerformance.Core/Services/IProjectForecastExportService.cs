using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectForecastExportService
{
    Task ExportToExcelAsync(ProjectForecastReportDto report, string filePath);
}
