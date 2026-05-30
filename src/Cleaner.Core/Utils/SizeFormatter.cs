using System.Globalization;

namespace Cleaner.Core.Utils;

/// <summary>Formats byte counts as human-readable sizes (binary units). No external dependency.</summary>
public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Humanize(long bytes)
    {
        if (bytes < 0)
        {
            return "-" + Humanize(-bytes);
        }

        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        // One decimal below 10, none above, to keep columns tidy.
        var format = size < 10 ? "0.0" : "0";
        return string.Create(CultureInfo.InvariantCulture, $"{size.ToString(format, CultureInfo.InvariantCulture)} {Units[unit]}");
    }
}
