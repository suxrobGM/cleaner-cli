using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class SizeParserTests
{
    [Theory]
    [InlineData("512", 512)]
    [InlineData("512 B", 512)]
    [InlineData("1.5 KB", 1536)]
    [InlineData("2 MB", 2 * 1024 * 1024)]
    [InlineData("1.50 GB", 1610612736)]
    [InlineData("  8.15 GB  ", 8750995865)]
    public void Parses_the_binary_units_dism_prints(string text, long expected)
    {
        Assert.True(SizeParser.TryParse(text, out var bytes));
        Assert.Equal(expected, bytes);
    }

    [Theory]
    [InlineData("3.2GB", 3_200_000_000)]
    [InlineData("800MB (66%)", 800_000_000)]
    [InlineData("0B (0%)", 0)]
    public void Parses_the_decimal_units_docker_prints(string text, long expected)
    {
        Assert.True(SizeParser.TryParse(text, out var bytes, unitBase: 1000));
        Assert.Equal(expected, bytes);
    }

    [Fact]
    public void An_explicit_binary_unit_wins_over_the_base()
    {
        Assert.True(SizeParser.TryParse("512MiB", out var bytes, unitBase: 1000));
        Assert.Equal(512 * 1024 * 1024, bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("N/A")]
    [InlineData("GB")]
    [InlineData("12 zz")]
    public void Rejects_what_it_cannot_read(string text)
    {
        Assert.False(SizeParser.TryParse(text, out var bytes));
        Assert.Equal(0, bytes);
    }
}
