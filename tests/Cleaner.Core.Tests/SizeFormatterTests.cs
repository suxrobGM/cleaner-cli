using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(15728640, "15 MB")]
    [InlineData(1073741824, "1.0 GB")]
    [InlineData(1099511627776, "1.0 TB")]
    public void Humanize_formats_binary_sizes(long bytes, string expected) =>
        Assert.Equal(expected, SizeFormatter.Humanize(bytes));

    [Fact]
    public void Humanize_handles_negative() =>
        Assert.Equal("-1.0 KB", SizeFormatter.Humanize(-1024));
}
