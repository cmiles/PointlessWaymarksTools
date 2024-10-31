using System.Reflection;

namespace PointlessWaymarks.CommonTools;

public static class EmbeddedResourceTools
{
    public static string GetEmbeddedResourceText(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}