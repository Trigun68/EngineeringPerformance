using EngineeringPerformance.Infrastructure.Data;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Запросы к версиям анализа в разрезе конкретного проекта.
/// Отделён от <see cref="EngineeringPerformance.Core.Services.IDirectoryService{T}"/>,
/// так как операция специфична для <see cref="ProjectAnalysisVersion"/> (фильтр по ProjectId)
/// и не имеет смысла для универсального справочного контракта.
/// </summary>
public interface IProjectAnalysisVersionQueryService
{
    /// <summary>
    /// Возвращает версии анализа конкретного проекта, упорядоченные от новых к старым.
    /// Загружает только необходимые связанные данные (тип физических объёмов),
    /// без дополнительного JOIN-а с проектом, так как проект уже известен.
    /// </summary>
    Task<IReadOnlyList<ProjectAnalysisVersion>> GetByProjectAsync(
        int projectId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);
}
