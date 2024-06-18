namespace PointlessWaymarks.VaultfuscationTools;

public static class ObfuscatedSettingsHelpers
{
    public static Func<T, (bool isValid, string message)> PropertyIsValidIfDirectoryExistsOrCanBeCreated<T>(
        Func<T, string> propertySelector)
    {
        return settings =>
        {
            if (string.IsNullOrWhiteSpace(propertySelector(settings))) return (false, "Value can not be blank.");

            try
            {
                var targetDirectory = new DirectoryInfo(propertySelector(settings));
                if (targetDirectory.Exists) return (true, string.Empty);
                targetDirectory.Create();
                targetDirectory.Refresh();
                if (targetDirectory.Exists) return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, $"Error with Directory {propertySelector(settings)}: {e.Message}");
            }

            return (true, string.Empty);
        };
    }

    public static Func<T, (bool isValid, string message)> PropertyIsValidIfFileExists<T>(
        Func<T, string> propertySelector)
    {
        return settings =>
        {
            if (string.IsNullOrWhiteSpace(propertySelector(settings))) return (false, "Value can not be blank.");

            try
            {
                var targetFile = new FileInfo(propertySelector(settings));
                if (targetFile.Exists) return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, $"Error with File {propertySelector(settings)}: {e.Message}");
            }

            return (false, $"File {propertySelector(settings)} does not exist?");
        };
    }

    public static Func<T, (bool isValid, string message)> PropertyIsValidIfFileExistsOrStringIsEmpty<T>(
        Func<T, string> propertySelector)
    {
        return settings =>
        {
            if (string.IsNullOrWhiteSpace(propertySelector(settings))) return (true, "");

            try
            {
                var targetFile = new FileInfo(propertySelector(settings));
                if (targetFile.Exists) return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, $"Error with File {propertySelector(settings)}: {e.Message}");
            }

            return (false, $"File {propertySelector(settings)} does not exist?");
        };
    }

    public static Func<T, (bool isValid, string message)> PropertyIsValidIfNotNullOrWhiteSpace<T>(
        Func<T, string> propertySelector)
    {
        return settings =>
        {
            if (string.IsNullOrWhiteSpace(propertySelector(settings))) return (false, "Value can not be blank.");

            return (true, string.Empty);
        };
    }

    public static Func<T, (bool isValid, string message)> PropertyIsValidIfPositiveInt<T>(Func<T, int> propertySelector)
    {
        return backupSettings =>
        {
            if (propertySelector(backupSettings) < 1) return (false, "Value must be a positive number.");

            return (true, string.Empty);
        };
    }

    public static Func<T, bool> ShouldSetPropertyIfNullOrWhiteSpace<T>(Func<T, string> propertySelector)
    {
        return settings =>
        {
            if (string.IsNullOrWhiteSpace(propertySelector(settings))) return true;

            return false;
        };
    }

    public static Func<string, (bool isValid, string message)> UserEntryIsValidIfBool()
    {
        return userEntry =>
        {
            if (string.IsNullOrWhiteSpace(userEntry)) return (false, "The value can not be blank.");

            if (!bool.TryParse(userEntry, out _)) return (false, "The value must be a boolean (true or false).");

            return (true, string.Empty);
        };
    }

    public static Func<string, (bool isValid, string message)> UserEntryIsValidIfInt()
    {
        return userEntry =>
        {
            if (string.IsNullOrWhiteSpace(userEntry)) return (false, "The value can not be blank.");

            if (!int.TryParse(userEntry, out _)) return (false, "The value must be a number.");

            return (true, string.Empty);
        };
    }

    public static Func<string, (bool isValid, string message)> UserEntryIsValidIfNotNullOrWhiteSpace()
    {
        return userEntry =>
        {
            if (string.IsNullOrWhiteSpace(userEntry)) return (false, "The value can not be blank.");

            return (true, string.Empty);
        };
    }
}