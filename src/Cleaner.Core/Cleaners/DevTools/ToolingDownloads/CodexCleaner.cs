using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Transient Codex CLI state and rotated sandbox logs under <c>~/.codex</c>. Sessions, memories,
/// skills, credentials, configuration, and the installed CLI are preserved.
/// </summary>
public sealed class CodexCleaner : DirectoryCleanerBase
{
    public override string Id => "codex";

    public override string Name => "Codex CLI scratch";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = OsPaths.Env(context.Environment, "CODEX_HOME") ?? context.Environment.HomePath(".codex");

        yield return new CleanupPath(Path.Combine(root, "cache"), DeleteMode.ClearContents, "cache");
        yield return new CleanupPath(Path.Combine(root, ".tmp"), DeleteMode.ClearContents, "temp");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents, "temp");

        foreach (var log in context.FileSystem.EnumerateFiles(root, "sandbox.*.log"))
        {
            yield return new CleanupPath(log, DeleteMode.DeleteFile, "sandbox log");
        }
    }
}
