using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectVersionAnalysisExportService
{
    Task ExportToExcelAsync(ProjectVersionAnalysisDto report, string filePath);
    Task ExportToPdfAsync(ProjectVersionAnalysisDto report, string filePath);
}
