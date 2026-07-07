using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cleaner.Cli.Rendering;

/// <summary>One target directory in the JSON scan report.</summary>
public sealed record JsonTarget(string Path, long Bytes, string? Description);

/// <summary>One cleaner in the JSON scan report.</summary>
public sealed record JsonCleaner(
    string Id,
    string Name,
    string Category,
    long Bytes,
    bool SizeIsEstimatable,
    IReadOnlyList<JsonTarget> Targets);

/// <summary>Root of the <c>scan --json</c> output.</summary>
public sealed record JsonScanReport(long TotalBytes, IReadOnlyList<JsonCleaner> Cleaners);

/// <summary>Machine-readable output for scripts and CI. Serialization is source-generated (AOT-safe).</summary>
public static class JsonOutput
{
    public static string Serialize(IReadOnlyList<ScanRow> rows)
    {
        var cleaners = rows
            .Select(r => new JsonCleaner(
                r.Cleaner.Id,
                r.Cleaner.Name,
                r.Cleaner.Category,
                r.Result.TotalBytes,
                !r.CommandBased,
                [.. r.Result.Targets.Select(t => new JsonTarget(t.Path, t.Bytes, t.Description))]))
            .ToList();

        var report = new JsonScanReport(rows.Sum(r => r.Result.TotalBytes), cleaners);
        return JsonSerializer.Serialize(report, CleanerJsonContext.Default.JsonScanReport);
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(JsonScanReport))]
internal sealed partial class CleanerJsonContext : JsonSerializerContext;
