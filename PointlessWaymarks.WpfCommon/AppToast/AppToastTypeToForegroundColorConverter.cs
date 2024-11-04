using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PointlessWaymarks.WpfCommon.AppToast;

public class AppToastTypeToForegroundColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is ToastType messageType)
            return messageType switch
            {
                ToastType.Success => new SolidColorBrush(Colors.White),
                ToastType.Error => new SolidColorBrush(Colors.White),
                ToastType.Info => new SolidColorBrush(Colors.White),
                ToastType.Warning => new SolidColorBrush(Colors.White),
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