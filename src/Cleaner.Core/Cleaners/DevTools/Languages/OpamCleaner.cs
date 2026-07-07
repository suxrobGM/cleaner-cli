using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>opam (OCaml) download cache; keeps switches and installed packages.</summary>
public sealed class OpamCleaner : DirectoryCleanerBase
{
    public override string Id => "opam";

    public override string Name => "opam download cache";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "OPAMROOT") ?? env.HomePath(".opam");
        yield return new CleanupPath(Path.Combine(root, "download-cache"));
    }
}
