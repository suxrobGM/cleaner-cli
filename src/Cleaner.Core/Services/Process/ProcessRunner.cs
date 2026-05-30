using System.ComponentModel;
using System.Diagnostics;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IProcessRunner"/>
public sealed class ProcessRunner : IProcessRunner
{
    public bool Exists(string executable) => TryResolve(executable, out _);

    public async Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Resolve to the concrete file so we launch the same thing Exists() reported. With
        // UseShellExecute=false, CreateProcess does not consult PATH/PATHEXT, so passing a bare
        // name (e.g. "npm") would throw Win32Exception(2) even though the tool is installed.
        var resolved = TryResolve(executable, out var fullPath) ? fullPath : executable;

        if (OperatingSystem.IsWindows() && IsBatchScript(resolved))
        {
            // Batch scripts (npm.cmd, yarn.cmd, …) are not PE images and can't be launched
            // directly via CreateProcess; route them through the command interpreter.
            startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(resolved);
        }
        else
        {
            startInfo.FileName = resolved;
        }

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            // Tool vanished between the availability check and launch, or isn't executable.
            // Surface as a failed result so callers can fall back instead of crashing.
            return new ProcessResult(-1, string.Empty, $"Failed to start '{executable}': {ex.Message}");
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var stdOut = await stdOutTask.ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);

        return new ProcessResult(process.ExitCode, stdOut, stdErr);
    }

    /// <summary>
    /// Resolves <paramref name="executable"/> to a concrete file path, honoring Windows PATHEXT.
    /// </summary>
    private static bool TryResolve(string executable, out string resolvedPath)
    {
        resolvedPath = executable;

        // Already a usable path (absolute or relative to the working directory).
        if (Path.IsPathRooted(executable) && File.Exists(executable))
        {
            return true;
        }

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
                    resolvedPath = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsBatchScript(string path) =>
        path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);
}
