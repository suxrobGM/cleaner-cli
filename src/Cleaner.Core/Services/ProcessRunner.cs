using System.Diagnostics;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IProcessRunner"/>
public sealed class ProcessRunner : IProcessRunner
{
    public bool Exists(string executable)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return false;
        }

        // On Windows, an unqualified name may resolve through any PATHEXT extension.
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, executable + ext);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdOut = await stdOutTask.ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }
}
