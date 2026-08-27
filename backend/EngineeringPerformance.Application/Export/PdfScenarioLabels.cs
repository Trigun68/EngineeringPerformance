namespace EngineeringPerformance.Application.Export;

/// <summary>
/// Полные русские названия сценариев трудоёмкости. Порядок всегда
/// оптимистичный → базовый → пессимистичный. Сокращения запрещены.
/// </summary>
public static class PdfScenarioLabels
{
    public const string Optimistic = "Оптимистичный";
    public const string Base = "Базовый";
    public const string Pessimistic = "Пессимистичный";

    /// <summary>Подпись сценария в сравнении версий, например «Оптимистичный (версия А)».</summary>
    public static string ForVersion(string scenario, string versionTag) => $"{scenario} ({versionTag})";

    public static string OptimisticA => ForVersion(Optimistic, "версия А");
    public static string BaseA => ForVersion(Base, "версия А");
    public static string PessimisticA => ForVersion(Pessimistic, "версия А");
    public static string OptimisticB => ForVersion(Optimistic, "версия Б");
    public static string BaseB => ForVersion(Base, "версия Б");
    public static string PessimisticB => ForVersion(Pessimistic, "версия Б");
}
