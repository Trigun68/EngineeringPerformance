using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EngineeringPerformance.App.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public sealed class InterpretationToBrushConverter : IValueConverter
{
    public Brush High { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
    public Brush Medium { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
    public Brush Low { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
    public Brush Default { get; set; } = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#95A5A6"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
            return Default;

        var lower = text.ToLowerInvariant();

        if (lower.Contains("высок") || lower.Contains("high") || lower.Contains("above"))
            return High;

        if (lower.Contains("низк") || lower.Contains("low") || lower.Contains("below"))
            return Low;

        if (lower.Contains("средн") || lower.Contains("medium") || lower.Contains("moderate") || lower.Contains("norm"))
            return Medium;

        return Default;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
