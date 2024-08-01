using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PointlessWaymarks.AvaloniaToolkit.AppToast;

public class AppToastTypeToBackgroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is ToastType messageType)
            return messageType switch
            {
                ToastType.Success => new SolidColorBrush(Colors.Green),
                ToastType.Error => new SolidColorBrush(Colors.Red),
                ToastType.Info => new SolidColorBrush(Colors.Blue),
                ToastType.Warning => new SolidColorBrush(Colors.Yellow),
                _ => new SolidColorBrush(Colors.Gray)
            };

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}