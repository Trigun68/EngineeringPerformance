namespace EngineeringPerformance.Application.Analytics.NormRegistry;

/// <summary>Рекомендации «что делать» по кодам правил health-check норм.</summary>
public static class NormHealthRecommendations
{
    private static readonly IReadOnlyDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GAP"] = "Проверьте непрерывность периодов действия норм; закройте разрыв новой версией или продлите EffectiveTo.",
            ["OVERLAP"] = "Устраните пересечение периодов: сократите EffectiveTo более ранней нормы или сдвиньте EffectiveFrom следующей.",
            ["SIGMA_ZERO"] = "Задайте ненулевое σ (StdDeviation) для нормы — иначе Z-оценка направления недоступна.",
            ["MISSING_PARAMS"] = "Заполните коэффициенты формулы и параметры отклонения; пересохраните норму.",
            ["FUTURE_STALE"] = "Подтвердите будущую норму или перенесите EffectiveFrom ближе к текущей дате.",
            ["NO_ACTIVE"] = "Создайте активную норму на конец периода анализа для направления IIC.",
            ["FORMULA_INVALID"] = "Исправьте тип формулы и параметры; сверьте с референсными формулами.",
        };

    public static string ForRule(string ruleCode) =>
        Map.TryGetValue(ruleCode, out var text)
            ? text
            : "Откройте карточку нормы, изучите детали правила и скорректируйте параметры или период действия.";
}
