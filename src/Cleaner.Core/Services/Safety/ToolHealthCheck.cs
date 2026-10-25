namespace Cleaner.Core.Services;

/// <summary>The state of the machine's developer tools at one moment.</summary>
/// <param name="WorkingTools">Tools whose version command succeeded, e.g. "git", "node".</param>
/// <param name="PathFiles">The files directly inside each existing PATH folder, keyed by folder.</param>
public sealed record ToolHealthSnapshot(
    IReadOnlySet<string> WorkingTools,
    IReadOnlyDictionary<string, IReadOnlySet<string>> PathFiles);

/// <summary>
/// Checks before and after a clean that PATH tools still start and no PATH folder lost files, so a
/// broken install is reported right away.
/// </summary>
public static class ToolHealthCheck
{
    /// <summary>Tools and the arguments that print their version without side effects.</summary>
    private static readonly (string Tool, string[] Arguments)[] Probes =
    [
        ("git", ["--version"]), ("node", ["--version"]), ("npm", ["--version"]), ("pnpm", ["--version"]),
        ("yarn", ["--version"]), ("bun", ["--version"]), ("deno", ["--version"]), ("dotnet", ["--version"]),
        ("cargo", ["--version"]), ("rustc", ["--version"]), ("go", ["version"]), ("python", ["--version"]),
        ("uv", ["--version"]), ("java", ["-version"]), ("scoop", ["--version"]), ("winget", ["--version"]),
    ];

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    public static async Task<ToolHealthSnapshot> CaptureAsync(
        IProcessRunner processRunner,
        IFileSystemService fileSystem,
        IReadOnlyList<string> pathDirectories,
        CancellationToken cancellationToken)
    {
        var probes = Probes
            .Where(p => processRunner.Exists(p.Tool))
            .Select(async p =>
            {
                var result = await processRunner.RunAsync(p.Tool, p.Arguments, ProbeTimeout, cancellationToken).ConfigureAwait(false);
                return (p.Tool, result.Success);
            });
        var outcomes = await Task.WhenAll(probes).ConfigureAwait(false);

        var files = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var directory in pathDirectories.Where(fileSystem.DirectoryExists))
        {
            files[directory] = fileSystem.EnumerateFiles(directory).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return new ToolHealthSnapshot(
            outcomes.Where(o => o.Success).Select(o => o.Tool).ToHashSet(StringComparer.OrdinalIgnoreCase),
            files);
    }

    /// <summary>Problems that appeared between <paramref name="before"/> and <paramref name="after"/>.</summary>
    public static IReadOnlyList<string> Regressions(ToolHealthSnapshot before, ToolHealthSnapshot after)
    {
        var problems = new List<string>();
        foreach (var tool in before.WorkingTools.Where(t => !after.WorkingTools.Contains(t)).Order(StringComparer.Ordinal))
        {
            problems.Add($"'{tool}' ran before the clean and fails now");
        }

        foreach (var (directory, files) in before.PathFiles)
        {
            if (!after.PathFiles.TryGetValue(directory, out var now))
            {
                problems.Add($"PATH folder {directory} disappeared");
                continue;
            }

            var lost = files.Where(f => !now.Contains(f)).ToList();
            if (lost.Count > 0)
            {
                problems.Add($"PATH folder {directory} lost {lost.Count} file(s), e.g. {Path.GetFileName(lost[0])}");
            }
        }

        return problems;
    }
}
