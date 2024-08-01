using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.AvaloniaToolkit.AppToast;

[NotifyPropertyChanged]
public partial class AppToastMessage
{
    public bool UserMustDismiss { get; set; }
    public string Message { get; set; } = string.Empty;
    public required DateTime AddedOn { get; set; }
    public required ToastType MessageType { get; set; }
}