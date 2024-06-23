using System.Diagnostics;
using System.Text;
using CliWrap;

namespace PointlessWaymarks.CommonTools;

public static class ProcessTools
{
    public static async Task<(bool success, string standardOutput, string errorOutput)> Execute(string programToExecute,
        string executionParameters, IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(programToExecute)) return (false, string.Empty, "Blank program to Execute?");

        var programToExecuteFile = new FileInfo(programToExecute);

        if (!programToExecuteFile.Exists)
            return (false, string.Empty, $"Program to Execute {programToExecuteFile} does not exist.");

        var standardOutput = new StringBuilder();
        var errorOutput = new StringBuilder();

        progress?.Report($"Setting up execution of {programToExecute} {executionParameters}");

        using var forcefulCts = new CancellationTokenSource();
        using var gracefulCts = new CancellationTokenSource();
        gracefulCts.CancelAfter(TimeSpan.FromSeconds(180));
        forcefulCts.CancelAfter(TimeSpan.FromSeconds(190));

        try
        {
            progress?.Report("Starting Process");

            var commandResult = await Cli.Wrap(programToExecuteFile.FullName)
                .WithArguments(executionParameters)
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x =>
                {
                    standardOutput.AppendLine(x);
                    progress?.Report(x);
                })).ExecuteAsync(forcefulCts.Token, gracefulCts.Token);

            return (commandResult.IsSuccess, standardOutput.ToString(), errorOutput.ToString());
        }
        catch (Exception e)
        {
            progress?.Report($"Error Running Process: {e.Message}");
        }

        return (false, standardOutput.ToString(), errorOutput.ToString());
    }

    public static void Open(string fileName)
    {
        var ps = new ProcessStartInfo(fileName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}