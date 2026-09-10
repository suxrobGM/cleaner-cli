# Contributing

## Build, test, run

```bash
dotnet build Cleaner.slnx -c Release          # build (warnings are errors)
dotnet test Cleaner.slnx -c Release           # run unit tests
dotnet run --project src/Cleaner.Cli          # launch the interactive menu
```

Use grouped Conventional Commit messages (for example,
`feat(cleaners): add Python cache cleaners`).

## Add a new cleaner

Example: a fictional tool `foo` whose cache is `~/.cache/foo`.

### 1. Write the class

Create it under `src/Cleaner.Core/Cleaners/` deriving from `DirectoryCleanerBase`, in the folder
for its ecosystem (see [architecture.md](architecture.md) for the layout). Resolve paths through the
injected `IEnvironmentService` — never hard-code OS paths.

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

If the tool's command is authoritative, derive from `ProcessCleanerBase` and add `Executable` and
`CleanArguments`. Declared directories, when present, provide sizing and the fallback when the tool
is unavailable.

For OS-specific or privileged cleaners, override `IsApplicable` (for example,
`context.Environment.IsWindows`) and `RequiresElevation`.

### 2. Register it

Add one line to the matching `ServiceCollectionExtensions.*.cs` partial under
`src/Cleaner.Cli/Infrastructure/`:

```csharp
services.AddSingleton<ICleaner, FooCleaner>();
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
