using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Projects;

/// <summary>
/// Правила смены направления при копировании версии анализа.
/// </summary>
public static class VersionCopyDirectionRules
{
    /// <summary>
    /// null → направление источника; Plan→Fact разрешён; иначе ошибка.
    /// </summary>
    public static AnalysisDirection Resolve(
        AnalysisDirection sourceDirection,
        AnalysisDirection? targetDirection)
    {
        if (targetDirection is null || targetDirection == sourceDirection)
            return sourceDirection;

        if (sourceDirection == AnalysisDirection.Plan && targetDirection == AnalysisDirection.Fact)
            return AnalysisDirection.Fact;

        throw new ArgumentException(
            $"Нельзя копировать версию {sourceDirection} → {targetDirection}. "
            + "Допускается только копирование в том же направлении или План → Факт.");
    }
}
