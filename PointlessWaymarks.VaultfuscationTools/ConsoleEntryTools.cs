namespace PointlessWaymarks.VaultfuscationTools;

public static class ConsoleEntryTools
{
    /// <summary>
    ///     INSECURE - This method is a simple way to get a * obscured string from the console
    ///     but there is no attempt to truly secure the string!
    /// </summary>
    /// <param name="displayMessage"></param>
    /// <returns></returns>
    public static string GetObscuredStringFromConsole(string displayMessage)
    {
        //https://stackoverflow.com/questions/3404421/password-masking-console-application
        var pass = string.Empty;
        Console.Write(displayMessage);
        ConsoleKeyInfo key;
        
        do
        {
            key = Console.ReadKey(true);
            
            // Backspace Should Not Work
            if (!char.IsControl(key.KeyChar))
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else
            {
                if (key.Key != ConsoleKey.Backspace || pass.Length <= 0) continue;
                
                pass = pass[..^1];
                Console.Write("\b \b");
            }
        }
        // Stops Receiving Keys Once Enter is Pressed
        while (key.Key != ConsoleKey.Enter);
        
        Console.WriteLine();
        
        return pass;
    }
}