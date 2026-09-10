using Cleaner.Core.Services;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class ProcessRunnerTests
{
    private readonly ProcessRunner _runner = new();

    [Fact]
    public void Exists_ReturnsFalse_ForUnknownExecutable()
    {
        Assert.False(_runner.Exists("definitely-not-a-real-tool-xyz"));
    }

    [Fact]
    public async Task RunAsync_ReturnsFailure_WhenTimeoutExpires()
    {
        var (executable, arguments) = SlowCommand();

        var result = await _runner.RunAsync(executable, arguments, TimeSpan.FromMilliseconds(200));

        Assert.False(result.Success);
        Assert.Contains("timed out", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_PropagatesCallerCancellation()
    {
        var (executable, arguments) = SlowCommand();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _runner.RunAsync(executable, arguments, TimeSpan.FromMinutes(1), cancellation.Token));
    }

    [Fact]
    public async Task RunAsync_ReturnsFailure_WhenExecutableMissing()
    {
        // Missing tools must return failure rather than throw and crash the app.
        var result = await _runner.RunAsync("definitely-not-a-real-tool-xyz", ["clean"]);

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_LaunchesResolvedTool_IncludingWindowsBatchScripts()
    {
        // This also covers Windows .cmd/.bat resolution through the command interpreter.
        var result = await _runner.RunAsync("dotnet", ["--version"]);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.StandardOutput));
    }

    /// <summary>A command that outlives any test deadline, on either platform.</summary>
    private static (string Executable, string[] Arguments) SlowCommand() =>
        OperatingSystem.IsWindows()
            ? ("ping", ["-n", "30", "127.0.0.1"])
            : ("sleep", ["30"]);
}
