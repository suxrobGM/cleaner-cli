using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// The C/C++ extension's regenerable IntelliSense store: precompiled headers (<c>ipch</c>) and
/// per-workspace symbol databases.
/// </summary>
public sealed class VsCppToolsCleaner : DirectoryCleanerBase
{
    public override string Id => "vscode-cpptools";

    public override string Name => "VS Code C/C++ IntelliSense cache";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = OsPaths.AppCache(
            context.Environment,
            Path.Combine("Microsoft", "vscode-cpptools"),
            "vscode-cpptools",
            "vscode-cpptools");

        yield return new CleanupPath(root, DeleteMode.ClearContents, "IntelliSense databases");
    }
}
