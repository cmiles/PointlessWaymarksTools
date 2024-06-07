using System.Text.Json;
using GitCredentialManager;
using Microsoft.Extensions.Logging;

namespace PointlessWaymarks.VaultfuscationTools;

public class ObfuscatedSettingsConsoleSetup<T>(ILogger<ObfuscatedSettingsConsoleSetup<T>> logger)
    where T : class, ISettingsFileType, new()
{
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
            Console.WriteLine();
            var userSettingsFileKey =
                ConsoleEntryTools.GetObscuredStringFromConsole("Please enter the settings file ObfuscationTools Key: ");
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
        
        foreach (var loopSettings in SettingsFileProperties)
        {
            var shouldHaveUserEnterValue = !loopSettings.PropertyIsValid(settings).isValid;
            
            while (shouldHaveUserEnterValue)
            {
                Console.WriteLine();
                
                if (!string.IsNullOrWhiteSpace(loopSettings.PropertyEntryHelp))
                    Console.WriteLine(loopSettings.PropertyEntryHelp);
                
                Console.Write($"{loopSettings.PropertyDisplayName}: ");
                
                var userEnteredValue = loopSettings.HideEnteredValue
                    ? ConsoleEntryTools.GetObscuredStringFromConsole(string.Empty)
                    : Console.ReadLine();
                
                var userEnteredValueIsValid = loopSettings.UserEntryIsValid(userEnteredValue ?? string.Empty);
                
                if (!userEnteredValueIsValid.isValid)
                {
                    if (!string.IsNullOrWhiteSpace(userEnteredValueIsValid.message))
                        Console.WriteLine(userEnteredValueIsValid.message);
                    continue;
                }
                
                loopSettings.SetValue(settings, userEnteredValue ?? string.Empty);
                
                var propertyIsValid = loopSettings.PropertyIsValid(settings);
                
                if (!propertyIsValid.isValid)
                {
                    if (!string.IsNullOrWhiteSpace(propertyIsValid.message)) Console.WriteLine(propertyIsValid.message);
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