using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

/// <param name="NewVersionName">Имя новой версии.</param>
/// <param name="Description">Описание (если null — берётся из источника).</param>
/// <param name="TargetDirection">
/// Целевое направление. null = как у источника.
/// Допускается Plan→Fact (ФО + оборудование; PLI не копируются — импорт трудоёмкости отдельно).
/// </param>
public sealed record CopyVersionRequest(
    string NewVersionName,
    string? Description = null,
    AnalysisDirection? TargetDirection = null);
