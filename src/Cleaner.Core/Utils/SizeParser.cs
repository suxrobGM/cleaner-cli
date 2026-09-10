using System.Globalization;

namespace Cleaner.Core.Utils;

/// <summary>Parses human-readable sizes emitted by external tools into bytes.</summary>
public static class SizeParser
{
    /// <summary>
    /// Parses a tool-formatted size. <paramref name="unitBase"/> applies to short units such as GB;
    /// explicit binary units such as GiB always use 1024. Trailing parenthetical text is ignored.
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
