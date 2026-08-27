namespace EngineeringPerformance.Domain.Norms;

/// <summary>
/// Уровень редактирования Z-параметров в диалоге планирования.
/// </summary>
public enum ZParameterLevel
{
    /// <summary>Три поля по проекту; каскад во все направления.</summary>
    ByProject = 1,

    /// <summary>Редактирование корней ИИС (уровень 1); потомки скрыты, каскад вниз.</summary>
    Level1 = 2,

    /// <summary>Редактирование уровней 1–2; более глубокие потомки скрыты.</summary>
    Level2 = 3,

    /// <summary>Все узлы ИИС видны и редактируемы.</summary>
    EachElement = 4,
}
