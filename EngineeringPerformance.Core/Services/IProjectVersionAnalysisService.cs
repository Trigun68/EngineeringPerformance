using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IProjectVersionAnalysisService
{
    Task<ProjectVersionAnalysisDto> GenerateReportAsync(int projectId, int versionId);
}
