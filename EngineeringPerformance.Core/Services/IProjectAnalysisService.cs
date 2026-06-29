using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectAnalysisService
{
    Task<ProjectAnalysisReportDto> GenerateReportAsync(int projectId, int versionId);
}
