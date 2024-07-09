using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.ConversionDataEntry;

public static class ConversionDataEntryHelpers
{
    public static (bool passed, string conversionMessage, DateTime value) DateTimeConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText))
            return (false, "Please enter a valid date", DateTime.MinValue);

        return DateTime.TryParse(cleanedUserText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue.TrimDateTimeToSeconds())
            : (false, $"Could not convert {userText} into an Date/Time?", DateTime.MinValue);
    }

    public static (bool passed, string conversionMessage, DateTime? value) DateTimeNullableConversion(
        string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

        return DateTime.TryParse(cleanedUserText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue.TrimDateTimeToSeconds())
            : (false, $"Could not convert {userText} into an Date/Time?", DateTime.MinValue);
    }

    public static (bool passed, string conversionMessage, double value) DoubleConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (false, "Please enter a valid number", 0D);

        return double.TryParse(cleanedUserText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
            : (false, $"Could not convert {userText} into an Number?", 0);
    }

    public static (bool passed, string conversionMessage, double? value) DoubleNullableConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

        return double.TryParse(cleanedUserText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
            : (false, $"Could not convert {userText} into an Number?", 0);
    }

    public static (bool passed, string conversionMessage, int value) IntConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (false, "Please enter a valid number", 0);

        return int.TryParse(userText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
            : (false, $"Could not convert {userText} into an Integer?", 0);
    }

    public static (bool passed, string conversionMessage, int value) IntGreaterThanZeroConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (false, "Please enter a valid number", 0);

        if (!int.TryParse(userText, out var parsedValue))
            return (false, $"Could not convert {userText} into an Integer?", 0);

        return parsedValue <= 0
            ? (false, $"Please enter a number greater than 0", 0)
            : (true, $"Converted {userText} to {parsedValue}", parsedValue);
    }

    public static (bool passed, string conversionMessage, int? value) IntNullableConversion(string userText)
    {
        var cleanedUserText = userText.TrimNullToEmpty();

        if (string.IsNullOrWhiteSpace(cleanedUserText)) return (true, "Found an Empty Value", null);

        return int.TryParse(cleanedUserText, out var parsedValue)
            ? (true, $"Converted {userText} to {parsedValue}", parsedValue)
            : (false, $"Could not convert {userText} into an Integer?", 0);
    }
}