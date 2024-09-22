using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Flurl.Http;
using Serilog;

namespace PointlessWaymarks.CommonTools;

public static class ProgramInfoTools
{
    public static DateTime? GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime;
    }

    public static DateTime? GetEntryAssemblyBuildDate()
    {
        try
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) return null;
            var attribute = entryAssembly.GetCustomAttribute<BuildDateAttribute>();
            return attribute?.DateTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    public static async Task<(string? dateString, string? setupFile)> LatestInstaller(string installerLocation,
        string baseInstallerName)
    {
        Log.Information("Latest Installer Called with Installer Location {0} and Base Installer Name {1}",
            installerLocation, baseInstallerName);

        if (string.IsNullOrEmpty(installerLocation))
        {
            Log.Debug("LatestInstaller Called with an empty installerLocation");
            return (null, null);
        }

        if (string.IsNullOrEmpty(baseInstallerName))
        {
            Log.Debug("LatestInstaller Called with an empty baseInstallerName");
            return (null, null);
        }

        if (installerLocation.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            string? jsonString;
            if (installerLocation.StartsWith("http"))
            {
                Log.Debug("Installer Json File starting with http?:// detected - {0} - getting json.",
                    installerLocation);

                jsonString = await installerLocation.GetStringAsync();
            }
            else
            {
                Log.Debug("Installer Json File starting without http?:// detected - {0} - reading file.",
                    installerLocation);
                jsonString = FileAndFolderTools.ReadAllText(installerLocation);
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                Log.Debug("Deserialized Installer Json File {0} is empty", installerLocation);
                return (null, null);
            }

            var fileList = JsonSerializer.Deserialize<List<ProgramInfoJson>>(jsonString);
            var latestInstaller = fileList
                ?.Where(x => x.InstallerBaseName.Equals(baseInstallerName, StringComparison.OrdinalIgnoreCase))
                .MaxBy(x => x.ApplicationVersion);

            Log.ForContext(nameof(fileList), fileList.SafeObjectDump())
                .ForContext(nameof(latestInstaller), latestInstaller.SafeObjectDump())
                .Information("Found {0} entries in the Json Installer File", fileList?.Count ?? 0);

            if (latestInstaller?.ApplicationVersion is null)
            {
                Log.Debug("No Latest Installer or Application Version Found for Latest Installer");
                return (null, null);
            }

            Log.ForContext(nameof(latestInstaller), latestInstaller.SafeObjectDump()).Information(
                "Installer Found - Version {0}, Installer {1}", latestInstaller.ApplicationVersion,
                latestInstaller.InstallerUrl);

            return (latestInstaller.ApplicationVersion.Value.ToString("yyyy-MM-dd-HH-mm"),
                latestInstaller.InstallerUrl);
        }

        Log.Debug("Installer Directory Style Reference Detected - {0}", installerLocation);
        var containingDirectory = new DirectoryInfo(installerLocation);

        if (!containingDirectory.Exists)
        {
            Log.Debug("Installer Directory {0} does not Exist", installerLocation);
            return (null, null);
        }

        var publishFile = containingDirectory.GetFiles($"{baseInstallerName}--*.exe").ToList().MaxBy(x => x.Name);

        if (publishFile == null)
        {
            Log.Debug("No Installer File Found in {0} for {1}", installerLocation, baseInstallerName);
            return (null, null);
        }

        var dateVersionString = Regex
            .Match(publishFile.Name, @".*--(?<dateVersion>\d\d\d\d-\d\d-\d\d-\d\d-\d\d).exe");

        if (!dateVersionString.Groups.TryGetValue("dateVersion", out var group))
        {
            Log.Debug("Installers found - {0} - but couldn't parse a Date Version from the File Name",
                publishFile.FullName);
            return (null, null);
        }

        Log.Information("Installer Found - Version {0}, Installer {1}", group.Value, publishFile.FullName);

        return (group.Value, publishFile.FullName);
    }

    public static (string humanTitleString, string dateVersion, bool isInstalled) StandardAppInformationString(
        string executingDirectory, string appName)
    {
        var humanTitleString = string.Empty;
        var foundInstallVersion = false;
        var dateVersionString = string.Empty;

        Log.Information("StandardAppInformationString ExecutingDirectory {ExecutingDirectory}, AppName {AppName}",
            executingDirectory, appName);

        try
        {
            humanTitleString += $"{appName}  ";

            if (!string.IsNullOrEmpty(executingDirectory))
            {
                var containingDirectory = new DirectoryInfo(executingDirectory);

                if (containingDirectory.Exists)
                {
                    var publishFile = containingDirectory.GetFiles("PublishVersion--*.txt").ToList().MaxBy(x => x.Name);


                    if (publishFile == null)
                    {
                        humanTitleString += " No Version Found";

                        Log.Information("StandardAppInformationString No Version Found");
                    }
                    else
                    {
                        foundInstallVersion = true;

                        humanTitleString +=
                            $" {Path.GetFileNameWithoutExtension(publishFile.Name).Split("--").LastOrDefault()}";

                        humanTitleString += $" {File.ReadAllText(publishFile.FullName)}";

                        dateVersionString = Regex
                            .Match(humanTitleString, @".* (?<dateVersion>\d\d\d\d-\d\d-\d\d-\d\d-\d\d) .*")
                            .Groups["dateVersion"].Value;

                        Log.ForContext("dateVersionString", dateVersionString)
                            .Information("StandardAppInformationString Found Version {Version}", humanTitleString);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Error in StandardAppInformationString");
            Console.WriteLine(e);
        }

        return (humanTitleString, dateVersionString, foundInstallVersion);
    }
}