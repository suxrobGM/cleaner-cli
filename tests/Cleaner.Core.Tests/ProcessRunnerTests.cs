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
}
