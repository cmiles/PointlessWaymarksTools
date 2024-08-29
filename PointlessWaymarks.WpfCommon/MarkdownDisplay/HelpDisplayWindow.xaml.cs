using System.Windows;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;

namespace PointlessWaymarks.WpfCommon.MarkdownDisplay;

/// <summary>
///     Interaction logic for HelpDisplayWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class HelpDisplayWindow : Window
{
    public HelpDisplayWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public required HelpDisplayContext HelpContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; } = "Help and Information";

    public static async Task CreateInstanceAndShow(List<string> markdown, string windowTitle)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new HelpDisplayWindow
        {
            HelpContext = new HelpDisplayContext(markdown), StatusContext = new StatusControlContext(),
            WindowTitle = windowTitle
        };

        await window.PositionWindowAndShowOnUiThread();
    }
}