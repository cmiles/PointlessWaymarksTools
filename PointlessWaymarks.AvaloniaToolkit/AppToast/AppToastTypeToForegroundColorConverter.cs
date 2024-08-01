using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PointlessWaymarks.AvaloniaToolkit.AppToast;

public class AppToastTypeToForegroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is ToastType messageType)
            return messageType switch
            {
                ToastType.Success => new SolidColorBrush(Colors.White),
                ToastType.Error => new SolidColorBrush(Colors.White),
                ToastType.Info => new SolidColorBrush(Colors.White),
                ToastType.Warning => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            };

        return new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object? value, Type targetType,
        object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}