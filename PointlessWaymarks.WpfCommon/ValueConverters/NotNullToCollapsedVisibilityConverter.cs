using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public sealed class NotNullToCollapsedVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => Visibility.Visible,
            _ => Visibility.Collapsed
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}