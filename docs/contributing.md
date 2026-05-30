# Contributing

## Build, test, run

```bash
dotnet build Cleaner.slnx -c Release          # build (warnings are errors)
dotnet test Cleaner.slnx -c Release           # run unit tests
dotnet run --project src/Cleaner.Cli -- list  # try it
```

Commits are grouped and use Conventional Commit messages
(e.g. `feat(cleaners): add Python cache cleaners`).

## Add a new cleaner

Most cleaners are a dozen lines. Say we want to clean a fictional tool `foo` whose cache lives at
`~/.cache/foo`.

### 1. Write the class

Create it under `src/Cleaner.Core/Cleaners/` deriving from `DirectoryCleanerBase`. Resolve paths
through the injected `IEnvironmentService` — never hard-code OS paths.

```csharp
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

public sealed class FooCleaner : DirectoryCleanerBase
{
    public override string Id => "foo";
    public override string Name => "Foo cache";
    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.Environment.CacheDirectory, "foo"), DeleteMode.ClearContents)];
}
```

If the tool's own command is the authoritative way to clean it, derive from `ProcessCleanerBase`
instead and add `Executable` + `CleanArguments`; the declared directories become the size source and
the fallback when the tool isn't installed.

For OS-specific or privileged cleaners, override `IsApplicable` (e.g.
`context.Environment.IsWindows`) and `RequiresElevation`.

### 2. Register it

Add one line in `src/Cleaner.Cli/Infrastructure/ServiceCollectionExtensions.cs`, in the matching
group:

```csharp
services.AddSingleton<ICleaner>(_ => new FooCleaner());
```

### 3. Test and document it

- Add a test under `tests/Cleaner.Core.Tests/` using `FakeFileSystem` / `TestContext`.
- Add a row to [docs/cleaners.md](cleaners.md).

## Guidelines

- **Safety first.** Only target caches, temp, and rebuildable artifacts. When in doubt, prefer
  `DeleteMode.ClearContents` and exclude anything that looks like user data.
- **Native-AOT clean.** No reflection or assembly scanning; register explicitly. The build runs the
  AOT/trim analyzers and treats warnings as errors.
- **Cross-platform.** Resolve every path via `IEnvironmentService`. Verify Windows/macOS/Linux
  branches.
