using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Дополнительные запросы к реестру норм ИИС (история по направлению и т.п.).
/// </summary>
public interface IIicWorkDirectionNormQueryService
{
    Task<IReadOnlyList<IicWorkDirectionNorm>> GetHistoryByWorkDirectionIdAsync(int workDirectionId);
}
