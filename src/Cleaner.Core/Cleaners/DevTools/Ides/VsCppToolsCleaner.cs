using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// The C/C++ extension's IntelliSense store: precompiled headers (<c>ipch</c>) plus one symbol
/// database per workspace. Regenerated on the next parse, and the largest cache VS Code produces.
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
