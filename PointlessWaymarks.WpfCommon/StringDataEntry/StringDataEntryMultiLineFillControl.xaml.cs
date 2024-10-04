using System.Windows;
using System.Windows.Controls;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

/// <summary>
///     Interaction logic for StringDataEntryMultiLineFillControl.xaml
/// </summary>
public partial class StringDataEntryMultiLineFillControl
{
    public static readonly DependencyProperty TextBoxHeightProperty = DependencyProperty.Register(nameof(TextBoxHeight),
        typeof(double), typeof(StringDataEntryMultiLineFillControl), new PropertyMetadata(default(double)));

    public StringDataEntryMultiLineFillControl()
    {
        InitializeComponent();
    }


    public double TextBoxHeight
    {
        get => (double)GetValue(TextBoxHeightProperty);
        set
        {
            {
                SetValue(TextBoxHeightProperty, value);
                ValueTextBox.Height = value;
            }
        }
    }
}