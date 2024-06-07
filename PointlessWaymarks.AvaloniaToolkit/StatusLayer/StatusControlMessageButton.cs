using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.AvaloniaToolkit.StatusLayer;

[NotifyPropertyChanged]
public partial class StatusControlMessageButton
{
    public bool IsDefault { get; set; }
    public string MessageText { get; set; } = string.Empty;
}