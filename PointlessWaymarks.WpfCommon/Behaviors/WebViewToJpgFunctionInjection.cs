using System.Reflection;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Xaml.Behaviors;
using OneOf;
using OneOf.Types;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.Behaviors;

public class WebViewToJpgFunctionInjection : Behavior<WebView2>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.DataContextChanged += (sender, _) =>
        {
            if (sender is WebView2 webView)
            {
                if (webView.DataContext is null) return;

                var dataContext = webView.DataContext;
                if (dataContext != null)
                {
                    var property = dataContext.GetType().GetProperty("JpgScreenshotFunction",
                        BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.PropertyType ==
                        typeof(Func<Task<OneOf<Success<byte[]>, Error<string>>>>))
                        property.SetValue(dataContext,
                            new Func<Task<OneOf<Success<byte[]>, Error<string>>>>(() =>
                                WebViewToJpg.SaveCurrentPageAsJpeg(webView)));
                }
            }
        };
    }
}