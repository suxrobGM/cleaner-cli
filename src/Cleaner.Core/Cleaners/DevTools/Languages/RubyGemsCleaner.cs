using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// RubyGems maintenance: <c>gem cleanup</c> removes superseded gem versions; the spec index cache is
/// deleted directly. Bundler's cache has its own cleaner.
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
