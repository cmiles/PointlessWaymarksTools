using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.StringDropdownDataEntry;

[NotifyPropertyChanged]
public partial class DropDownDataChoice
{
    public string DataString { get; set; } = string.Empty;
    public string DisplayString { get; set; } = string.Empty;
}