﻿using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Action = System.Action;

namespace PointlessWaymarks.AvaloniaToolkit.Utility;

public class ListBoxAutoScrollToNewItems : Behavior<ListBox>
{
    private INotifyCollectionChanged? _cachedItemsSource;

    private void AssociatedObjectLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateItemsSource();
    }

    private void ItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?[0] != null)
            try
            {
                Dispatcher.UIThread.InvokeAsync((Action)(() =>
                {
                    if (AssociatedObject is null) return;
                    AssociatedObject.ScrollIntoView(e.NewItems[0]!);
                    AssociatedObject.SelectedItem = e.NewItems[0];
                }), DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        UpdateItemsSource();

        if (AssociatedObject is null) return;

        AssociatedObject.Loaded += AssociatedObjectLoaded;
    }

    private void UpdateItemsSource()
    {
        if (AssociatedObject?.ItemsSource is INotifyCollectionChanged sourceCollection)
        {
            if (_cachedItemsSource != null) _cachedItemsSource.CollectionChanged -= ItemsSourceCollectionChanged;
            _cachedItemsSource = sourceCollection;
            if (_cachedItemsSource == null) return;
            _cachedItemsSource.CollectionChanged += ItemsSourceCollectionChanged;
        }
    }
}