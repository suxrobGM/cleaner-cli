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

        // Resolve the path explicitly because CreateProcess does not consult PATH/PATHEXT here.
        var resolved = TryResolve(executable, out var fullPath) ? fullPath : executable;

        if (OperatingSystem.IsWindows() && IsBatchScript(resolved))
        {
            // Windows batch scripts must run through the command interpreter.
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
            // The tool may have disappeared since the availability check; return a failed result.
            return new ProcessResult(-1, string.Empty, $"Failed to start '{executable}': {ex.Message}");
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stdErrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // WaitForExitAsync does not stop the child. A timed-out scan (or Ctrl+C) must not
            // leave docker or another redirected console process alive in the background.
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // The process may already have exited, or access may be denied.
            }

            // Observe the redirected reads, which use the same cancelled token.
            try
            {
                await Task.WhenAll(stdOutTask, stdErrTask).ConfigureAwait(false);
            }
            catch
            {
                // The original cancellation is rethrown below.
            }

            throw;
        }

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
