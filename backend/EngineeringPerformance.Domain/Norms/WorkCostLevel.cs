namespace EngineeringPerformance.Domain.Norms;

/// <summary>
/// Уровень задания стоимости работ (те же глубины, что <see cref="ZParameterLevel"/>).
/// В БД не хранится — только для формы ввода и каскада на сервере.
/// </summary>
public enum WorkCostLevel
{
    /// <summary>Одна ставка на всю организацию; каскад во все направления ИИС.</summary>
    Organization = 1,

    /// <summary>Редактирование корней ИИС (уровень 1); каскад в потомков.</summary>
    Level1 = 2,

    /// <summary>Редактирование уровней 1–2; каскад в более глубоких потомков.</summary>
    Level2 = 3,

    /// <summary>Каждый узел ИИС задаётся отдельно, без каскада.</summary>
    EachElement = 4,
}
