using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class PathComparisonTests
{
    [Fact]
    public void Normalize_unifies_separators()
    {
        var mixed = $"root{Path.AltDirectorySeparatorChar}child";
        var native = $"root{Path.DirectorySeparatorChar}child";

        Assert.Equal(native, PathComparison.Normalize(mixed));
        Assert.Equal(native, PathComparison.Normalize(native));
    }

    [Fact]
    public void Windows_and_macOS_paths_ignore_case()
    {
        var set = PathComparison.CreateSet(["/work/Cache"], isLinux: false);

        Assert.Contains(PathComparison.Normalize("/WORK/cache"), set);
    }

    [Fact]
    public void Linux_paths_are_case_sensitive()
    {
        var set = PathComparison.CreateSet(["/work/Cache"], isLinux: true);

        Assert.DoesNotContain(PathComparison.Normalize("/work/cache"), set);
        Assert.Contains(PathComparison.Normalize("/work/Cache"), set);
    }
}
