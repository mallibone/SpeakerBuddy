using System.Diagnostics;
using System.Text;

namespace StageBuddy.App.EndToEndTests;

sealed record CliResult(int ExitCode, string StdOut, string StdErr)
{
    public string Combined => string.IsNullOrEmpty(StdErr) ? StdOut : $"{StdOut}\n{StdErr}";
}

/// <summary>Runs external processes (dotnet, xcrun, maui) with captured output and a hard timeout.</summary>
static class Cli
{
    public static CliResult Run(
        string fileName,
        IReadOnlyList<string> arguments,
        string? stdin = null,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin is not null,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };
        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdOut.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stdErr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (stdin is not null)
        {
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }

        if (!process.WaitForExit((int)(timeout ?? TimeSpan.FromMinutes(3)).TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            throw new TimeoutException($"'{fileName} {string.Join(' ', arguments)}' timed out.");
        }
        process.WaitForExit(); // flush async output handlers

        return new CliResult(process.ExitCode, stdOut.ToString(), stdErr.ToString());
    }

    public static CliResult RunChecked(
        string fileName,
        IReadOnlyList<string> arguments,
        string? stdin = null,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        var result = Run(fileName, arguments, stdin, workingDirectory, timeout);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"'{fileName} {string.Join(' ', arguments)}' exited with {result.ExitCode}:\n{result.Combined}");
        return result;
    }
}
