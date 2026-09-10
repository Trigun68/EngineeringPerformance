using EngineeringPerformance.Domain.Norms;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Распределение целевого объёма группы IIC по строкам PPV в зависимости от сценария планирования.
/// Не обнуляет sibling: масштабирует текущие доли физобъёма.
/// </summary>
public static class PlannedVolumeDistributor
{
    public const string EachValueMultipleRowsMessage =
        "В сценарии «по каждому значению» задайте объёмы в детализации физических объёмов, а не одним числом по группе IIC.";

    /// <summary>
    /// Возвращает новые <c>Quantity</c> (единицы каталога) в том же порядке, что <paramref name="rows"/>.
    /// </summary>
    public static IReadOnlyList<decimal> Distribute(
        PlanningScenario scenario,
        decimal targetIicVolume,
        IReadOnlyList<(decimal Coefficient, decimal CurrentQuantity)> rows)
    {
        if (targetIicVolume < 0m)
            throw new ArgumentOutOfRangeException(nameof(targetIicVolume), "Плановый объём не может быть отрицательным.");
        if (rows.Count == 0)
            throw new ArgumentException("Нет строк для распределения объёма.", nameof(rows));

        if (scenario == PlanningScenario.ByEachValue)
        {
            if (rows.Count > 1)
                throw new InvalidOperationException(EachValueMultipleRowsMessage);

            return [ToCatalogQuantity(targetIicVolume, rows[0].Coefficient)];
        }

        var currentIic = new decimal[rows.Count];
        decimal sum = 0m;
        for (var i = 0; i < rows.Count; i++)
        {
            var k = PositiveCoeff(rows[i].Coefficient);
            currentIic[i] = rows[i].CurrentQuantity * k;
            sum += currentIic[i];
        }

        var result = new decimal[rows.Count];
        decimal assignedIic = 0m;
        for (var i = 0; i < rows.Count; i++)
        {
            decimal shareIic;
            if (i == rows.Count - 1)
            {
                shareIic = targetIicVolume - assignedIic;
            }
            else if (sum <= 0m)
            {
                shareIic = targetIicVolume / rows.Count;
            }
            else
            {
                shareIic = targetIicVolume * (currentIic[i] / sum);
            }

            result[i] = ToCatalogQuantity(shareIic, rows[i].Coefficient);
            assignedIic += result[i] * PositiveCoeff(rows[i].Coefficient);
        }

        return result;
    }

    private static decimal PositiveCoeff(decimal coefficient) =>
        coefficient <= 0m ? 1m : coefficient;

    private static decimal ToCatalogQuantity(decimal volumeInIicUnits, decimal catalogCoefficient) =>
        volumeInIicUnits / PositiveCoeff(catalogCoefficient);
}
