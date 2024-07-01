using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

[NotifyPropertyChanged]
public partial class StringDataEntryNoIndicatorsContext : IHasChanges, IHasValidationIssues
{
    private StringDataEntryNoIndicatorsContext()
    {
    }

    public int BindingDelay { get; set; } = 10;
    public string HelpText { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UserValue { get; set; } = string.Empty;
    public List<Func<string?, Task<IsValid>>> ValidationFunctions { get; set; } = [];
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges { get; set; }
    public bool HasValidationIssues { get; set; }

    public Task CheckForChangesAndValidationIssues()
    {
        return Task.CompletedTask;
    }

    public static StringDataEntryNoIndicatorsContext CreateInstance()
    {
        return new StringDataEntryNoIndicatorsContext();
    }
}