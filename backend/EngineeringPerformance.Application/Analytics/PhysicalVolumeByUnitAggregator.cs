using System.Globalization;
using System.Text;

namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Суммирование физ. объёмов по одинаковым единицам измерения для строк «Итого».
/// Формат: «12 345 (ед.изм.); …» — целое с разделителем разрядов, без дробной части.
/// </summary>
public static class PhysicalVolumeByUnitAggregator
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static IReadOnlyList<(string Unit, decimal Total)> Aggregate(
        IEnumerable<(string? Unit, decimal? Volume)> items)
    {
        var sums = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var (unit, volume) in items)
        {
            if (volume is not > 0)
                continue;
            if (string.IsNullOrWhiteSpace(unit))
                continue;
            var key = unit.Trim();
            sums[key] = sums.TryGetValue(key, out var existing) ? existing + volume.Value : volume.Value;
        }

        return sums
            .Select(kv => (
                Unit: kv.Key,
                Total: Math.Round(kv.Value, 0, MidpointRounding.AwayFromZero)))
            .OrderBy(x => x.Unit, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<(string Unit, decimal Total)> AggregateFromPerformance(
        IEnumerable<PerformanceRowDto> rows) =>
        Aggregate(rows.Select(r => (r.NormUnitName, r.FactPhysicalVolume)));

    public static string Format(IReadOnlyList<(string Unit, decimal Total)> totals)
    {
        if (totals.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (var i = 0; i < totals.Count; i++)
        {
            if (i > 0)
                sb.Append("; ");
            var (unit, total) = totals[i];
            sb.Append(FormatNumber(total));
            sb.Append(" (");
            sb.Append(unit);
            sb.Append(')');
        }

        return sb.ToString();
    }

    public static string? FormatOrNull(IEnumerable<(string? Unit, decimal? Volume)> items)
    {
        var totals = Aggregate(items);
        var text = Format(totals);
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public static string? FormatFromPerformance(IEnumerable<PerformanceRowDto> rows) =>
        FormatOrNull(rows.Select(r => (r.NormUnitName, r.FactPhysicalVolume)));

    private static string FormatNumber(decimal value) =>
        value.ToString("N0", DisplayCulture);
}
