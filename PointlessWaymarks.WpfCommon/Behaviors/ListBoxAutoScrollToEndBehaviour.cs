using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class ListBoxAutoScrollToEndBehaviour : Behavior<ListBox>
{
    private INotifyCollectionChanged? _cachedItemsSource;
    private ScrollViewer? _scrollViewer;

    private void AssociatedObjectLoaded(object sender, RoutedEventArgs e)
    {
        UpdateItemsSource();
    }

    private void ItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_scrollViewer is null)
        {
            _scrollViewer = XamlHelpers.GetDescendantByType(AssociatedObject, typeof(ScrollViewer)) as ScrollViewer;
            if (_scrollViewer is not null)
                _scrollViewer.ScrollChanged += (o, args) =>
                {
                    if (args.ExtentHeightChange != 0)
                    {
                        if (Thread.CurrentThread == Application.Current.Dispatcher.Thread)
                            _scrollViewer.ScrollToBottom();
                        else
                            Application.Current.Dispatcher.Invoke(() => _scrollViewer.ScrollToBottom());
                    }
                };
        }
    }

    private void ListBoxItemsSourceChanged(object? sender, EventArgs e)
    {
        UpdateItemsSource();
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        UpdateItemsSource();

        AssociatedObject.Loaded += AssociatedObjectLoaded;

        var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(AssociatedObject)["ItemsSource"];

        Debug.Assert(itemsSourcePropertyDescriptor != null, nameof(itemsSourcePropertyDescriptor) + " != null");
        itemsSourcePropertyDescriptor.AddValueChanged(AssociatedObject, ListBoxItemsSourceChanged);
    }

    private void UpdateItemsSource()
    {
        if (_cachedItemsSource != null)
        {
            _cachedItemsSource.CollectionChanged -= ItemsSourceCollectionChanged;
            var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(AssociatedObject)["ItemsSource"];

            Debug.Assert(itemsSourcePropertyDescriptor != null, nameof(itemsSourcePropertyDescriptor) + " != null");
            itemsSourcePropertyDescriptor.RemoveValueChanged(AssociatedObject, ListBoxItemsSourceChanged);
        }

        if (AssociatedObject.ItemsSource is INotifyCollectionChanged sourceCollection)
        {
            _cachedItemsSource = sourceCollection;
            if (_cachedItemsSource == null) return;
            _cachedItemsSource.CollectionChanged += ItemsSourceCollectionChanged;
            var itemsSourcePropertyDescriptor = TypeDescriptor.GetProperties(AssociatedObject)["ItemsSource"];

            Debug.Assert(itemsSourcePropertyDescriptor != null, nameof(itemsSourcePropertyDescriptor) + " != null");
            itemsSourcePropertyDescriptor.AddValueChanged(AssociatedObject, ListBoxItemsSourceChanged);
        }
    }
}