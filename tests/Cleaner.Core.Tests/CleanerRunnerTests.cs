using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class CleanerRunnerTests
{
    private readonly CleanupContext _context = new()
    {
        FileSystem = new FakeFileSystem(),
        Environment = new FakeEnvironment(),
        ProcessRunner = new FakeProcessRunner(),
    };

    [Fact]
    public async Task SafeCleanAsync_TurnsThrowIntoErrorResult()
    {
        var cleaner = new ThrowingCleaner(new InvalidOperationException("boom"));

        var result = await CleanerRunner.SafeCleanAsync(cleaner, _context, new NullAppLogger());

        Assert.True(result.HasErrors);
        Assert.Contains("boom", result.Errors[0]);
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public async Task SafeScanAsync_TurnsThrowIntoEmptyResult()
    {
        var cleaner = new ThrowingCleaner(new IOException("disk gone"));

        var result = await CleanerRunner.SafeScanAsync(cleaner, _context, new NullAppLogger());

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public async Task SafeCleanAsync_PropagatesCancellation()
    {
        var cleaner = new ThrowingCleaner(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CleanerRunner.SafeCleanAsync(cleaner, _context, new NullAppLogger()));
    }

    private sealed class ThrowingCleaner(Exception toThrow) : ICleaner
    {
        public string Id => "throwing";

        public string Name => "Throwing";

        public string Category => "Test";

        public bool RequiresElevation => false;

        public bool IsApplicable(CleanupContext context) => true;

        public bool IsAvailable(CleanupContext context) => true;

        public Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default) =>
            throw toThrow;

        public Task<CleanResult> CleanAsync(
            CleanupContext context,
            IProgress<CleanProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw toThrow;
    }
}
