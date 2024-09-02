using System.ComponentModel;
using PointlessWaymarks.CommonTools.S3;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;

namespace PointlessWaymarks.WpfCommon.S3Uploads;

/// <summary>
///     Interaction logic for S3UploadsWindow.xaml
/// </summary>
[NotifyPropertyChanged]
[StaThreadConstructorGuard]
public partial class S3UploadsWindow
{
    public S3UploadsWindow()
    {
        InitializeComponent();

        DataContext = this;
    }

    public bool ForceClose { get; set; }
    public required StatusControlContext StatusContext { get; set; }
    public S3UploadsContext? UploadContext { get; set; }
    public WindowIconStatus? WindowStatus { get; set; }
    public required string WindowTitle { get; set; }

    public static async Task<S3UploadsWindow> CreateInstance(IS3AccountInformation s3Info, List<S3UploadRequest> toLoad,
        string windowTitleNote,
        bool autoStartUpload = false)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var window = new S3UploadsWindow
        {
            StatusContext = await StatusControlContext.CreateInstance(),
            WindowStatus = new WindowIconStatus(),
            WindowTitle = string.IsNullOrWhiteSpace(windowTitleNote) ? "S3 Uploads" : $"S3 Uploads - {windowTitleNote}"
        };

        window.StatusContext.RunFireAndForgetBlockingTask(async () =>
        {
            window.UploadContext =
                await S3UploadsContext.CreateInstance(window.StatusContext, s3Info, toLoad, window.WindowStatus);
            if (autoStartUpload)
                window.UploadContext.StatusContext.RunNonBlockingTask(window.UploadContext.StartAllUploads);
        });

        return window;
    }

    private void S3UploadsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        if (ForceClose) return;

        StatusContext.RunFireAndForgetNonBlockingTask(WindowCloseOverload);
        e.Cancel = true;
    }

    public async Task WindowCloseOverload()
    {
        if (UploadContext?.UploadBatch is not { Uploading: true })
        {
            ForceClose = true;
            await ThreadSwitcher.ResumeForegroundAsync();
            Close();
        }

        var userAction = await StatusContext.ShowMessage("Running Upload...",
            "Exiting this window with an upload running could create errors on S3:",
            ["Close Immediately", "Cancel and Close", "Return to Upload"]);

        switch (userAction)
        {
            case "Close Immediately":
            {
                ForceClose = true;
                await ThreadSwitcher.ResumeForegroundAsync();
                Close();
                break;
            }
            case "Return and Cancel":
            {
                UploadContext?.UploadBatch?.Cancellation?.Cancel();
                break;
            }
        }
    }
}