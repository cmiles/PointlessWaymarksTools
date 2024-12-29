using System.Windows;

namespace PointlessWaymarks.WpfCommon.ConversionDataEntry;

/// <summary>
///     Interaction logic for ConversionDataEntryControl.xaml
/// </summary>
public partial class ConversionDataEntryControl
{
    public static readonly DependencyProperty ValueTextBoxWidthProperty =
        DependencyProperty.Register(
            nameof(ValueTextBoxWidth),
            typeof(double),
            typeof(ConversionDataEntryControl),
            new PropertyMetadata(double.NaN, ValueTextBoxWidthChangedCallback));

    public ConversionDataEntryControl()
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
        if (d is not ConversionDataEntryControl control || e.NewValue is not double widthValue) return;
        control.ValueTextBox.HorizontalAlignment = HorizontalAlignment.Left;
        control.ValueTextBox.Width = widthValue;
    }
}