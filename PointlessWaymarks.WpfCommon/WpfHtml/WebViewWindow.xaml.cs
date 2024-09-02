using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;

namespace PointlessWaymarks.WpfCommon.WpfHtml;

/// <summary>
///     Interaction logic for WebViewWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class WebViewWindow : IWebViewMessenger
{
    public WebViewWindow()
    {
        InitializeComponent();

        FromWebView = new WorkQueue<FromWebViewMessage>();
        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        DataContext = this;
    }

    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = "Pointless Waymarks";
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public static async Task<WebViewWindow> CreateInstance()
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        return new WebViewWindow { StatusContext = await StatusControlContext.CreateInstance() };
    }
}