using System.Windows;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

/// <summary>
///     Interaction logic for StringDataEntryMultiLineControl.xaml
/// </summary>
public partial class StringDataEntryMultiLineControl
{
    public static readonly DependencyProperty ValueTextBoxHeightProperty = DependencyProperty.Register(
        nameof(ValueTextBoxHeight),
        typeof(double), typeof(StringDataEntryMultiLineControl),
        new PropertyMetadata(double.NaN, ValueTextBoxHeightChangedCallback));

    public static readonly DependencyProperty ValueTextBoxWidthProperty =
        DependencyProperty.Register(
            nameof(ValueTextBoxWidth),
            typeof(double),
            typeof(StringDataEntryMultiLineControl),
            new PropertyMetadata(double.NaN, ValueTextBoxWidthChangedCallback));

    public StringDataEntryMultiLineControl()
    {
        InitializeComponent();
    }

    public double ValueTextBoxHeight
    {
        get => (double)GetValue(ValueTextBoxHeightProperty);
        set
        {
            SetValue(ValueTextBoxHeightProperty, value);
            ValueTextBox.Height = value;
        }
    }

    public double ValueTextBoxWidth
    {
        get => (double)GetValue(ValueTextBoxWidthProperty);
        set
        {
            SetValue(ValueTextBoxWidthProperty, value);
            ValueTextBox.HorizontalAlignment = HorizontalAlignment.Left;
            ValueTextBox.Width = value;
        }
    }

    private static void ValueTextBoxHeightChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StringDataEntryMultiLineControl control || e.NewValue is not double heightValue) return;
        control.ValueTextBox.Height = heightValue;
    }

    private static void ValueTextBoxWidthChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StringDataEntryMultiLineControl control || e.NewValue is not double widthValue) return;
        control.ValueTextBox.HorizontalAlignment = HorizontalAlignment.Left;
        control.ValueTextBox.Width = widthValue;
    }
}