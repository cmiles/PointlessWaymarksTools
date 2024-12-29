using System.Windows;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

/// <summary>
///     Interaction logic for StringDataEntryControl.xaml
/// </summary>
public partial class StringDataEntryControl
{
    public static readonly DependencyProperty ValueTextBoxWidthProperty =
        DependencyProperty.Register(
            nameof(ValueTextBoxWidth),
            typeof(double),
            typeof(StringDataEntryControl),
            new PropertyMetadata(double.NaN, ValueTextBoxWidthChangedCallback));

    public StringDataEntryControl()
    {
        InitializeComponent();
    }

    public double ValueTextBoxWidth
    {
        get => (double)GetValue(ValueTextBoxWidthProperty);
        set => SetValue(ValueTextBoxWidthProperty, value);
    }

    private static void ValueTextBoxWidthChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StringDataEntryControl control || e.NewValue is not double widthValue) return;
        control.ValueTextBox.HorizontalAlignment = HorizontalAlignment.Left;
        control.ValueTextBox.Width = widthValue;
    }
}