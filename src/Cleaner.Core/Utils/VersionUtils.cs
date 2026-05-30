using System.Globalization;
using Cleaner.Core.Services;

namespace Cleaner.Core.Utils;

/// <summary>
/// Pure helpers for comparing semantic versions and picking the right release asset. Kept separate
/// from <see cref="IUpdateService"/> so the fiddly parsing logic is unit-testable in isolation.
/// </summary>
public static class VersionUtils
{
    /// <summary>Strip a leading <c>v</c> and any <c>+build</c> metadata, e.g. <c>v1.2.3+abc</c> → <c>1.2.3</c>.</summary>
    public static string Normalize(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        var trimmed = version.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }

        var plus = trimmed.IndexOf('+');
        return plus >= 0 ? trimmed[..plus] : trimmed;
    }

    /// <summary>
    /// Compare two semantic versions. Returns &lt;0 if <paramref name="a"/> precedes
    /// <paramref name="b"/>, 0 if equal, &gt;0 otherwise. A pre-release (e.g. <c>1.0.0-rc1</c>) ranks
    /// below its release (<c>1.0.0</c>), per SemVer.
    /// </summary>
    public static int Compare(string a, string b)
    {
        var (releaseA, preA) = SplitPreRelease(Normalize(a));
        var (releaseB, preB) = SplitPreRelease(Normalize(b));

        var coreCompare = CompareNumericSegments(releaseA, releaseB);
        if (coreCompare != 0)
        {
            return coreCompare;
        }

        // Equal core: no pre-release outranks a pre-release; otherwise compare pre-release tags.
        if (preA.Length == 0 && preB.Length == 0)
        {
            return 0;
        }

        if (preA.Length == 0)
        {
            return 1;
        }

        if (preB.Length == 0)
        {
            return -1;
        }

        return string.CompareOrdinal(preA, preB);
    }

    /// <summary>True if <paramref name="latest"/> is strictly newer than <paramref name="current"/>.</summary>
    public static bool IsNewer(string latest, string current) => Compare(latest, current) > 0;

    /// <summary>
    /// Pick the asset whose file name contains the runtime identifier (e.g. <c>win-x64</c>), or null
    /// when no asset matches this platform.
    /// </summary>
    public static UpdateAsset? SelectAsset(IEnumerable<GitHubAsset> assets, string runtimeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(runtimeIdentifier))
        {
            return null;
        }

        foreach (var asset in assets)
        {
            if (asset.Name.Contains(runtimeIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateAsset(asset.Name, asset.BrowserDownloadUrl);
            }
        }

        return null;
    }

    private static (string Release, string PreRelease) SplitPreRelease(string version)
    {
        var dash = version.IndexOf('-');
        return dash >= 0 ? (version[..dash], version[(dash + 1)..]) : (version, string.Empty);
    }

    private static int CompareNumericSegments(string a, string b)
    {
        var segmentsA = a.Split('.');
        var segmentsB = b.Split('.');
        var length = Math.Max(segmentsA.Length, segmentsB.Length);

        for (var i = 0; i < length; i++)
        {
            var valueA = i < segmentsA.Length ? ParseSegment(segmentsA[i]) : 0;
            var valueB = i < segmentsB.Length ? ParseSegment(segmentsB[i]) : 0;
            if (valueA != valueB)
            {
                return valueA.CompareTo(valueB);
            }
        }

        return 0;
    }

    private static int ParseSegment(string segment) =>
        int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
