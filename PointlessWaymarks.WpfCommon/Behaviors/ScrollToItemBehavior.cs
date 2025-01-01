using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace PointlessWaymarks.WpfCommon.Behaviors;

/// <summary>
///     This behaviour watches the ScrollRequestItem property and when it changes it tries to scroll
///     the item into view in the associated ListBox. This behavior tries to ignore any/all errors.
///     The intended use is to bind the ScrollRequestItem to a property in a ViewModel and then only
///     update the ViewModel property when you want to make a ScrollRequest -> this might be a more
///     natural fit for an Event rather than a property binding so beware of the non-standard usage!
/// </summary>
public class ScrollToItemBehavior : Behavior<ListBox>
{
    //Inspiration from https://stackoverflow.com/questions/43763831/binding-the-delay-property-of-a-binding
    public static readonly DependencyProperty ScrollRequestItemProperty =
        DependencyProperty.Register(nameof(ScrollRequestItem), typeof(object),
            typeof(ScrollToItemBehavior), new PropertyMetadata(null, OnScrollRequestItemChanged));

    public ScrollToItemRequest? ScrollRequestItem
    {
        get => (ScrollToItemRequest?)GetValue(ScrollRequestItemProperty);
        set => SetValue(ScrollRequestItemProperty, value);
    }

    private static async void OnScrollRequestItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var source = d as ScrollToItemBehavior;
        var attached = source?.AssociatedObject;
        if (attached is null) return;
        if (source!.ScrollRequestItem?.ScrollTo is null) return;

        if (attached.Items.Count == 0) return;

        foreach (var loopItems in attached.Items)
            if (loopItems != null && loopItems.Equals(source.ScrollRequestItem.ScrollTo))
                try
                {
                    await ThreadSwitcher.ResumeForegroundAsync();
                    attached.ScrollIntoView(loopItems);
                }
                catch
                {
                    // ignored
                }
    }
}