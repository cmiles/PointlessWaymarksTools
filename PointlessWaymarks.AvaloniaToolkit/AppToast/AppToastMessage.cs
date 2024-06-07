using PointlessWaymarks.AvaloniaToolkit.Aspects;

namespace PointlessWaymarks.AvaloniaToolkit.AppToast;

[NotifyPropertyChanged]
public partial class AppToastMessage
{
    public bool UserMustDismiss { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime AddedOn { get; set; }
}