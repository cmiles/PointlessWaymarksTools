﻿using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PointlessWaymarks.WpfCommon.ValueConverters;

public class VisibleWhenAllFalseOtherwiseCollapsedMultiConverter : IMultiValueConverter
{
    public object? Convert(object[]? values, Type? targetType, object? parameter, CultureInfo culture)
    {
        if (values == null) return Visibility.Collapsed;
        if (values.Length == 0) return Visibility.Collapsed;

        foreach (var valueLoop in values)
        {
            if (valueLoop is false) continue;
            return Visibility.Collapsed;
        }

        return Visibility.Visible;
    }

    public object[]? ConvertBack(object? value, Type[]? targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}