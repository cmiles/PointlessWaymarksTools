using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.VaultfuscationTools;

public static class VaultfuscationMessages
{
    public static void VaultfuscationWarning()
    {
        ConsoleTools.WriteRedWrappedTextBlock("""
                                              This program stores your Garmin Connect login information in an Obfuscated Settings File. THIS FILE CAN BE ACCESSED AND READ BY ANYONE WITH ACCESS TO THIS USER ACCOUNT! In some cases this is an appropriate amount of security - but it is up to you to understand and accept the risks associated with this strategy.

                                              """);
    }

    public static void VaultfuscationWarningAndUserAcknowledgement()
    {
        VaultfuscationWarning();

        ConsoleTools.WriteWrappedTextBlock(
            """Please press 'Y' to accept the risks and continue, press any other key to decline and quit: """);

        var userChoice = Console.ReadKey();

        if (userChoice.Key != ConsoleKey.Y) Environment.Exit(-1);
    }
}