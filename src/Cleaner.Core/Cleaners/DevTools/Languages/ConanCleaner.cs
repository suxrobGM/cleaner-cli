using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Conan (C/C++) package cache. <c>conan cache clean "*"</c> removes source/build/download/temp
/// folders but keeps package binaries; with <c>--force</c> the cached packages themselves are
/// removed too (re-downloaded or rebuilt on the next install).
/// </summary>
public sealed class ConanCleaner : ProcessCleanerBase
{
    public override string Id => "conan";

    public override string Name => "Conan cache";

    public override string Category => Categories.Languages;

    protected override string Executable => "conan";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean", "*"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return CleanArguments;
        if (context.Force)
        {
            yield return ["remove", "*", "--confirm"];
        }
    }

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var conanHome = OsPaths.Env(env, "CONAN_HOME") ?? env.HomePath(".conan2");
        yield return new CleanupPath(Path.Combine(conanHome, "p"), Description: "Conan 2 package cache");
        yield return new CleanupPath(env.HomePath(".conan", "data"), Description: "Conan 1 package cache");
    }
}
