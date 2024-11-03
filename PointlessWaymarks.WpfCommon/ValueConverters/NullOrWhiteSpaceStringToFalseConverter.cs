using System.Globalization;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public class NullOrWhiteSpaceStringToFalseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value?.ToString())) return false;

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}