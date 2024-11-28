using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class WebViewToJpgFunctionInjection : Behavior<WebView2>
{
    public static readonly DependencyProperty JpgScreenshotFunctionNameProperty = DependencyProperty.Register(
        nameof(JpgScreenshotFunctionName), typeof(string), typeof(WebViewToJpgFunctionInjection),
        new PropertyMetadata("JpgScreenshotFunctionName", PropertyChangedCallback));

    private WebView2? _webView;

    public string JpgScreenshotFunctionName
    {
        get => (string)GetValue(JpgScreenshotFunctionNameProperty);
        set => SetValue(JpgScreenshotFunctionNameProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.DataContextChanged += (sender, _) =>
        {
            _webView = sender as WebView2;

            TryInjectFunction();
        };
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WebViewToJpgFunctionInjection possibleContainer) return;

        possibleContainer.TryInjectFunction();
    }

    private void TryInjectFunction()
    {
        var dataContext = _webView?.DataContext;

        if (dataContext is null) return;

        var property = dataContext.GetType().GetProperty(JpgScreenshotFunctionName,
            BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.PropertyType ==
            typeof(Func<Task<OneOf<Success<byte[]>, Error<string>>>>))
            property.SetValue(dataContext,
                new Func<Task<OneOf<Success<byte[]>, Error<string>>>>(() =>
                    WebViewToJpg.SaveCurrentPageAsJpeg(_webView!)));
    }
}