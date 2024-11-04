using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PointlessWaymarks.WpfCommon.AppToast;

public class AppToastTypeToBackgroundColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        if (value is ToastType messageType)
            return messageType switch
            {
                ToastType.Success => new SolidColorBrush(Colors.LimeGreen),
                ToastType.Error => new SolidColorBrush(Colors.OrangeRed),
                ToastType.Info => new SolidColorBrush(Colors.RoyalBlue),
                ToastType.Warning => new SolidColorBrush(Colors.Orange),
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