using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// RubyGems spec index cache, re-fetched on the next install. Installed gems are kept:
/// <c>gem cleanup</c> would uninstall older versions a <c>Gemfile.lock</c> may still pin. Bundler has
/// its own cleaner.
/// </summary>
public sealed class RubyGemsCleaner : DirectoryCleanerBase
{
    public override string Id => "rubygems";

    public override string Name => "RubyGems spec cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".gem", "specs"), DeleteMode.ClearContents, "spec index")];
}
