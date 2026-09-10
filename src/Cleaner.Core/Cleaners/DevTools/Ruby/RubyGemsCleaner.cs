using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Runs <c>gem cleanup</c> for superseded versions and clears the spec index cache. Bundler has its
/// own cleaner.
/// </summary>
public sealed class RubyGemsCleaner : ProcessCleanerBase
{
    public override string Id => "rubygems";

    public override string Name => "RubyGems old versions";

    public override string Category => Categories.Languages;

    protected override string Executable => "gem";

    protected override IReadOnlyList<string> CleanArguments => ["cleanup"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".gem", "specs"), DeleteMode.ClearContents, "spec index")];
}
