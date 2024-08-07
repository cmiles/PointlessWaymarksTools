using System.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.StringDataEntry;

[NotifyPropertyChanged]
public partial class StringDataEntryNoChangeIndicatorContext : IHasChanges, IHasValidationIssues
{
    private StringDataEntryNoChangeIndicatorContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public int BindingDelay { get; set; } = 10;
    public string HelpText { get; set; } = string.Empty;
    public string ReferenceValue { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string UserValue { get; set; } = string.Empty;
    public List<Func<string?, Task<IsValid>>> ValidationFunctions { get; set; } = [];
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges => false;
    public bool HasValidationIssues { get; set; }

    public async Task CheckForChangesAndValidationIssues()
    {
        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = await loopValidations(UserValue);
                if (!validationResult.Valid)
                {
                    HasValidationIssues = true;
                    ValidationMessage = validationResult.Explanation;
                    return;
                }
            }

        HasValidationIssues = false;
        ValidationMessage = string.Empty;
    }

    public static StringDataEntryNoChangeIndicatorContext CreateInstance()
    {
        return new StringDataEntryNoChangeIndicatorContext();
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(ReferenceValue)) || e.PropertyName.Equals(nameof(UserValue)) ||
            e.PropertyName.Equals(nameof(ValidationFunctions)))
            await CheckForChangesAndValidationIssues();
    }
}