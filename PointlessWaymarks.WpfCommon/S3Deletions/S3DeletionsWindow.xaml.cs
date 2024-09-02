using System.ComponentModel;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Deletions;

/// <summary>
///     Interaction logic for S3DeletionsWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class S3DeletionsWindow
{
    public S3DeletionsWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public S3DeletionsContext? DeletionContext { get; set; }
    public required StatusControlContext StatusContext { get; set; }

    public static async Task<S3DeletionsWindow> CreateInstance(IS3AccountInformation s3Info,
        List<S3DeletionsItem> itemsToDelete)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new S3DeletionsWindow {StatusContext = await StatusControlContext.CreateInstance()};

        window.StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            window.DeletionContext = await S3DeletionsContext.CreateInstance(window.StatusContext, s3Info, itemsToDelete);
        });

        return window;
    }

    private void S3DeletionsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (!StatusContext.BlockUi) return;

        e.Cancel = true;
    }
}