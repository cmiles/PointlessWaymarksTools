using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class DataEntryControlTextBoxWidthBindingBehavior : Behavior<UserControl>
{
    public static readonly DependencyProperty WidthBindingProperty =
        DependencyProperty.Register(nameof(WidthBinding), typeof(int?),
            typeof(DataEntryControlTextBoxWidthBindingBehavior),
            new PropertyMetadata(null, OnBindingPropertyChanged));

    public static readonly DependencyProperty HorizontalAlignmentBindingProperty = DependencyProperty.Register(
        nameof(HorizontalAlignmentBinding), typeof(HorizontalAlignment?),
        typeof(DataEntryControlTextBoxWidthBindingBehavior),
        new PropertyMetadata(null, OnBindingPropertyChanged));


    public HorizontalAlignment? HorizontalAlignmentBinding
    {
        get => (HorizontalAlignment)GetValue(HorizontalAlignmentBindingProperty);
        set => SetValue(HorizontalAlignmentBindingProperty, value);
    }

    public int? WidthBinding
    {
        get => (int?)GetValue(WidthBindingProperty);
        set => SetValue(WidthBindingProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        var target = AssociatedObject?.FindName("ValueTextBox");

        if (target is TextBox targetTextBox)
        {
            if (WidthBinding != null) targetTextBox.Width = WidthBinding.Value;
            if (HorizontalAlignmentBinding != null)
                targetTextBox.HorizontalAlignment = HorizontalAlignmentBinding.Value;
        }
    }

    private static void OnBindingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataEntryControlTextBoxWidthBindingBehavior source) return;

        var attached = source.AssociatedObject;
        var target = attached?.FindName("ValueTextBox");

        if (target is TextBox targetTextBox)
        {
            if (source.WidthBinding != null) targetTextBox.Width = source.WidthBinding.Value;
            if (source.HorizontalAlignmentBinding != null)
                targetTextBox.HorizontalAlignment = source.HorizontalAlignmentBinding.Value;
        }
    }
}