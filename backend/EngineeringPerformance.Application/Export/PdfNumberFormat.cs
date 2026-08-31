using System.Globalization;

namespace EngineeringPerformance.Application.Export;

/// <summary>
/// Числа визуальных PDF: ru-RU (пробел групп, запятая дробной части),
/// физобъём/трудоёмкость — 1 знак, производительность и Z — 2 знака.
/// </summary>
public static class PdfNumberFormat
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public const int VolumeDecimals = 1;
    public const int HoursDecimals = 1;
    public const int ProductivityDecimals = 2;
    public const int ZDecimals = 2;

    public static string Volume(decimal? value) => Format(value, VolumeDecimals);

    public static string Hours(decimal? value) => Format(value, HoursDecimals);

    public static string HoursWithUnit(decimal? value) =>
        value is null ? "—" : $"{Hours(value)} ч";

    /// <summary>Стоимость на титуле PDF: целые рубли, ru-RU группировка, суффикс ₽. null — «—».</summary>
    public static string CurrencyRub(decimal? value)
    {
        if (value is null)
            return "—";
        var rounded = decimal.Round(value.Value, 0, MidpointRounding.AwayFromZero);
        return $"{rounded.ToString("N0", Ru)} ₽";
    }

    public static string Productivity(decimal? value) => Format(value, ProductivityDecimals);

    public static string Z(decimal? value) => Format(value, ZDecimals);

    public static string SignedZ(decimal? value)
    {
        if (value is null)
            return "—";
        var formatted = Format(Math.Abs(value.Value), ZDecimals);
        if (value.Value > 0m)
            return $"+{formatted}";
        if (value.Value < 0m)
            return $"−{formatted}";
        return formatted;
    }

    public static string Format(decimal? value, int decimals)
    {
        if (value is null)
            return "—";
        return value.Value.ToString($"N{decimals}", Ru);
    }

    public static string Format(double? value, int decimals)
    {
        if (value is null)
            return "—";
        return value.Value.ToString($"N{decimals}", Ru);
    }
}
