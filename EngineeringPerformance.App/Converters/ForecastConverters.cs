using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EngineeringPerformance.App.Converters;

/// <summary>bool (IsTotal) → FontStyle Italic/Normal</summary>
public sealed class BoolToFontStyleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? FontStyles.Italic : FontStyles.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>bool (IsTotal) → фоновый цвет строки итога</summary>
public sealed class BoolToTotalBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? new SolidColorBrush(Color.FromRgb(0xEB, 0xF5, 0xFB)) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
