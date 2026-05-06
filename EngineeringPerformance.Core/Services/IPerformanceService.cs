using EngineeringPerformance.Core.DTOs;

namespace EngineeringPerformance.Core.Services;

public interface IPerformanceService
{
    Task<IReadOnlyList<PerformanceRowDto>> GetPerformanceDataAsync(int projectId, int versionId);
}
