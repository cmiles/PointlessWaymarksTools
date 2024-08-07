using System.ComponentModel;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.ChangesAndValidation;

namespace PointlessWaymarks.WpfCommon.BoolDataEntry;

[NotifyPropertyChanged]
public partial class BoolDataEntryNoChangeIndicatorContext : IHasChanges, IHasValidationIssues
{
    private BoolDataEntryNoChangeIndicatorContext()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public string HelpText { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool ReferenceValue { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool UserValue { get; set; }
    // ReSharper disable once UnusedMember.Global
    public bool UserValueIsNullable => false;
    public List<Func<bool, IsValid>> ValidationFunctions { get; set; } = [];
    public string ValidationMessage { get; set; } = string.Empty;
    public bool HasChanges => false;
    public bool HasValidationIssues { get; set; }

    public void CheckForChangesAndValidate()
    {
        if (ValidationFunctions.Any())
            foreach (var loopValidations in ValidationFunctions)
            {
                var validationResult = loopValidations(UserValue);
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

    public static Task<BoolDataEntryNoChangeIndicatorContext> CreateInstance()
    {
        return Task.FromResult(new BoolDataEntryNoChangeIndicatorContext());
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

        if (e.PropertyName.Equals(nameof(ReferenceValue)) || e.PropertyName.Equals(nameof(UserValue)) ||
            e.PropertyName.Equals(nameof(ValidationFunctions)) || e.PropertyName.Equals(nameof(IsEnabled)))
            CheckForChangesAndValidate();
    }
}