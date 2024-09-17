namespace PointlessWaymarks.CommonTools;

public class ProgramInfoJson
{
    public string ApplicationName { get; set; } = string.Empty;
    public string InstallerFileName { get; set; } = string.Empty;
    public string InstallerFileSha1 { get; set; } = string.Empty;
    public string InstallerBaseName { get; set; } = string.Empty;
    public DateTime? ApplicationVersion { get; set; }
    public string InstallerUrl { get; set; } = string.Empty;
}