using System.Text.Json;
using GitCredentialManager;
using Microsoft.Extensions.Logging;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.VaultfuscationTools;

public class ObfuscatedSettingsConsoleSetup<T>(ILogger<ObfuscatedSettingsConsoleSetup<T>> logger)
    where T : class, ISettingsFileType, new()
{
    /// <summary>
    ///     Determines if the setup process should be interactive - true by default - if false
    ///     the program will not create new settings files or prompt the user for input with
    ///     missing or invalid values. This is useful for automated processes.
    /// </summary>
    public bool Interactive { get; set; } = true;

    private ILogger<ObfuscatedSettingsConsoleSetup<T>> Log { get; } = logger;

    /// <summary>
    ///     Full Path to the Settings File - if the file does not exist it will be created.
    /// </summary>
    public string SettingsFile { get; set; } = string.Empty;

    /// <summary>
    ///     A string identifier for the 'type' of settings file - this helps to verify that the
    ///     de-obfuscated settings file is the correct type and valid.
    /// </summary>
    public string SettingsFileIdentifier { get; set; } = string.Empty;

    public List<SettingsFileProperty<T>> SettingsFileProperties { get; set; } = [];

    /// <summary>
    ///     The Vault Account - this will be used along with the 'Vault Service Identifier' to
    ///     retrieve the obfuscation key. If multiple users/accounts are not expected for the
    ///     settings file (a personal utility console program for example) then the default should
    ///     suffice.
    /// </summary>
    public string VaultAccount { get; set; } = "AutomatedUserSettings";

    /// <summary>
    ///     The Vault Service Identifier is used to store the obfuscation key in the Windows Credential Manager.
    ///     This will be used along with the 'Vault Account' to retrieve the obfuscation key.
    ///     One strategy is to use a URL like 'http://myprogramname.test'.
    /// </summary>
    public string VaultServiceIdentifier { get; set; } = string.Empty;

    public async Task<(bool isValid, T settings)> Setup()
    {
        if (string.IsNullOrWhiteSpace(SettingsFile))
        {
            Log.LogError("A non-Blank/Empty Settings File must be provided");
            return (false, new T());
        }

        var store = CredentialManager.Create();

        var settingsFileKey = store.Get(VaultServiceIdentifier, VaultAccount);
        var obfuscationKey = settingsFileKey?.Password ?? string.Empty;

        if (settingsFileKey == null || string.IsNullOrWhiteSpace(settingsFileKey.Password))
        {
            if (!Interactive)
            {
                Log.LogError(
                    "No Obfuscation Key found in the Windows Credential Manager for {vaultService} - ObfuscatedSettingsConsoleSetup is Not in Interactive Mode - exiting.",
                    VaultServiceIdentifier);
                return (false, new T());
            }

            Console.WriteLine();
            var userSettingsFileKey =
                ConsoleTools.GetObscuredStringFromConsole("Please enter the Obfuscation Key (password) to be used for these settings: ");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(userSettingsFileKey))
            {
                Log.LogError("Sorry - a non-blank ObfuscationTools Key must provided... exiting.");
                return (false, new T());
            }

            store.AddOrUpdate(VaultServiceIdentifier, "AutomatedUserSettings", userSettingsFileKey);
            obfuscationKey = userSettingsFileKey;
        }

        var settingsFile = new FileInfo(SettingsFile);

        if (!settingsFile.Exists)
        {
            if (!Interactive)
            {
                Log.LogError(
                    "The Settings File {settingsFile} was not found - ObfuscatedSettingsConsoleSetup is Not in Interactive Mode - exiting.",
                    settingsFile.FullName);
                return (false, new T());
            }

            Log.LogInformation($"Settings File {settingsFile.FullName} does not exist - creating it now.");
            var newSettings = JsonSerializer.Serialize(new T());
            var newSettingsJsonObfuscated = newSettings.Encrypt(obfuscationKey);
            await File.WriteAllTextAsync(settingsFile.FullName, newSettingsJsonObfuscated);
        }

        var settingsFileContentsObfuscated = await File.ReadAllTextAsync(settingsFile.FullName);
        var settingsFileContents = settingsFileContentsObfuscated.Decrypt(obfuscationKey);
        var settings = JsonSerializer.Deserialize<T>(settingsFileContents);

        if (settings == null)
        {
            Log.LogError("Could not read the Settings File {fileName}", settingsFile.FullName);
            return (false, new T());
        }

        if (string.IsNullOrWhiteSpace(settings.SettingsType) ||
            !settings.SettingsType.Equals(SettingsFileIdentifier))
        {
            Log.LogError("{fileName} could not be read as a settings file - wrong key? wrong file?",
                settingsFile.FullName);
            return (false, new T());
        }

        if (!Interactive)
        {
            var hasValidationIssues = false;
            var propertyNameWithValidation = new List<string>();

            foreach (var loopSettings in SettingsFileProperties)
                if (!loopSettings.PropertyIsValid(settings).isValid)
                {
                    hasValidationIssues = true;
                    propertyNameWithValidation.Add(loopSettings.PropertyDisplayName);
                }

            if (hasValidationIssues)
            {
                Log.LogError("Settings File {settingsFile} has validation issues with: {invalidProperties}",
                    settingsFile.FullName, string.Join(", ", propertyNameWithValidation));
                return (false, settings);
            }

            return (true, settings);
        }

        foreach (var loopSettings in SettingsFileProperties)
        {
            var shouldHaveUserEnterValue = !loopSettings.PropertyIsValid(settings).isValid;

            var defaultValue = loopSettings.ShowCurrentSettingAsDefault ? loopSettings.GetCurrentStringValue(settings) : string.Empty;

            while (shouldHaveUserEnterValue)
            {
                Console.WriteLine();

                if (!string.IsNullOrWhiteSpace(loopSettings.PropertyEntryHelp))
                    Console.WriteLine($"{loopSettings.PropertyDisplayName}: {loopSettings.PropertyEntryHelp}");

                Console.Write($"Value for {loopSettings.PropertyDisplayName}: ");

                var userEnteredValue = loopSettings.HideEnteredValue
                    ? ConsoleTools.GetObscuredStringFromConsole(string.Empty)
                    : ConsoleTools.ReadLine(defaultValue);

                defaultValue = userEnteredValue;

                var userEnteredValueIsValid = loopSettings.UserEntryIsValid(userEnteredValue ?? string.Empty);

                if (!userEnteredValueIsValid.isValid)
                {
                    if (!string.IsNullOrWhiteSpace(userEnteredValueIsValid.message))
                        ConsoleTools.WriteLineYellowWrappedTextBlock(userEnteredValueIsValid.message, indent: 4);
                    continue;
                }

                loopSettings.SetValue(settings, userEnteredValue ?? string.Empty);

                var propertyIsValid = loopSettings.PropertyIsValid(settings);

                if (!propertyIsValid.isValid)
                {
                    if (!string.IsNullOrWhiteSpace(propertyIsValid.message)) ConsoleTools.WriteLineYellowWrappedTextBlock(propertyIsValid.message, indent: 4);
                    continue;
                }

                shouldHaveUserEnterValue = false;
            }
        }

        var currentSettingsJson = JsonSerializer.Serialize(settings);
        var currentSettingsJsonObfuscated = currentSettingsJson.Encrypt(obfuscationKey);
        await File.WriteAllTextAsync(settingsFile.FullName, currentSettingsJsonObfuscated);

        return (true, settings);
    }
}