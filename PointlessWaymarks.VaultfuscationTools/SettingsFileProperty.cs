namespace PointlessWaymarks.VaultfuscationTools;

public class SettingsFileProperty<T>
{
    public Func<T, string> GetCurrentStringValue { get; set; } = _ => string.Empty;

    /// <summary>
    ///     Turns the entered values to '*' characters - useful for passwords. THIS HAS NOTHING
    ///     TO DO WITH 'SECURE STRINGS' - this is just a visual obfuscation, examining the memory
    ///     of the running program will reveal this value in plain text!!
    /// </summary>
    public bool HideEnteredValue { get; set; } = false;

    /// <summary>
    ///     The 'name' for the property that will be used in the console
    ///     prompts if user data entry is needed.
    /// </summary>
    public string PropertyDisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     Non-blank values are displayed to the user if they are prompted
    ///     to enter a value for this property.
    /// </summary>
    public string PropertyEntryHelp { get; set; } = string.Empty;

    /// <summary>
    ///     Used to check if a settings property value is valid - this is called for both properties
    ///     read from an existing file and after the property is set with 'SetValue'. For user entry
    ///     the sequence is 'UserEntryIsValid', valid values set with 'SetValue' and then 'PropertyIsValid'.
    /// </summary>
    public Func<T, (bool isValid, string message)> PropertyIsValid { get; set; } = _ => (true, string.Empty);

    /// <summary>
    ///     If a user's entry is valid as determined by 'UserEntryIsValid' then this method
    ///     will set the value of the property and then 'PropertyIsValid' will be called.
    /// </summary>
    public Action<T, string> SetValue { get; set; } = (_, _) => { };

    /// <summary>
    ///     Determines whether the current setting value is input for the user as a default value. This
    ///     does NOT apply if HideEnteredValue is true (if a value is worth hiding there is no reason
    ///     to allow the user to enter it by default!). This defaults to true.
    /// </summary>
    public bool ShowCurrentSettingAsDefault { get; set; } = true;


    /// <summary>
    ///     Determines whether the property should be shown to the user for entry. This defaults to true. This
    ///     can be used to skip properties that only make sense if previous/other properties have a particular
    ///     setting - for example if 'Write Backups' is false then 'Backup Directory' should not be shown to the user.
    /// </summary>
    public Func<T, bool> ShowForUserEntry { get; set; } = _ => true;

    /// <summary>
    ///     Called after a user enters a value for a property - this should check if the
    ///     user's entry can be converted to the correct type and if it is a valid value. If
    ///     a user's entry is valid as determined by this method the value will be set with
    ///     'SetValue' (and then 'PropertyIsValid' will be called).
    /// </summary>
    public Func<string, (bool isValid, string message)> UserEntryIsValid { get; set; } = _ => (true, string.Empty);
}