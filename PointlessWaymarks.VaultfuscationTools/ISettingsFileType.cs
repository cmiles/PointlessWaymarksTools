namespace PointlessWaymarks.VaultfuscationTools;

public interface ISettingsFileType
{
    /// <summary>
    ///     A string identifier for the 'type' of settings file - this helps
    ///     verify that the de-obfuscated settings file is the correct type and
    ///     valid.
    /// </summary>
    public string SettingsType { get; set; }
}