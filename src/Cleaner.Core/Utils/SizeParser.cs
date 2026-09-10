using System.Globalization;

namespace Cleaner.Core.Utils;

/// <summary>
/// Reads the human sizes external tools print ("3.2GB", "8.15 GB", "512 MiB") back into bytes, so a
/// cleaner that can only learn its size from a command's output still reports a real number.
/// </summary>
public static class SizeParser
{
    /// <summary>
    /// Parse <paramref name="text"/> as a size. <paramref name="unitBase"/> says what a plain "GB"
    /// means to the tool that printed it — Docker counts in 1000s, DISM in 1024s — while an explicit
    /// "GiB" is always 1024. Anything after a "(" is ignored, so Docker's "3.2GB (62%)" parses.
    /// </summary>
    public static bool TryParse(string text, out long bytes, int unitBase = 1024)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Split('(')[0].Trim();
        var split = 0;
        while (split < value.Length && (char.IsAsciiDigit(value[split]) || value[split] is '.' or ',' or '-'))
        {
            split++;
        }

        var number = value[..split].Replace(",", string.Empty, StringComparison.Ordinal);
        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        if (!TryMultiplier(value[split..].Trim(), unitBase, out var multiplier))
        {
            return false;
        }

        var scaled = amount * multiplier;
        if (scaled is < 0 or > long.MaxValue)
        {
            return false;
        }

        bytes = (long)scaled;
        return true;
    }

    private static bool TryMultiplier(string unit, int unitBase, out double multiplier)
    {
        // "GiB" and friends are unambiguous; the short forms follow the printing tool's convention.
        var binary = unit.Contains('i', StringComparison.OrdinalIgnoreCase);
        double scale = binary ? 1024 : unitBase;
        var prefix = unit.Length == 0 ? '\0' : char.ToLowerInvariant(unit[0]);

        multiplier = prefix switch
        {
            '\0' or 'b' => 1,
            'k' => scale,
            'm' => scale * scale,
            'g' => scale * scale * scale,
            't' => scale * scale * scale * scale,
            'p' => scale * scale * scale * scale * scale,
            _ => 0,
        };

        return multiplier > 0;
    }
}
