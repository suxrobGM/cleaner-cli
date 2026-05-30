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
    public async Task RunAsync_ReturnsFailure_WhenExecutableMissing()
    {
        // A missing tool must degrade to a failed result, not throw Win32Exception and crash the app.
        var result = await _runner.RunAsync("definitely-not-a-real-tool-xyz", ["clean"]);

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_LaunchesResolvedTool_IncludingWindowsBatchScripts()
    {
        // dotnet resolves to a real executable on every dev/CI machine; on Windows the same code
        // path also covers .cmd/.bat resolution since they route through the command interpreter.
        var result = await _runner.RunAsync("dotnet", ["--version"]);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.StandardOutput));
    }
}
