# Contributing

## Build, test, run

```bash
dotnet build Cleaner.slnx -c Release   # warnings are errors
dotnet test Cleaner.slnx -c Release
dotnet run --project src/Cleaner.Cli
```

Use focused [Conventional Commits](https://www.conventionalcommits.org/), for example
`feat(cleaners): add Python cache cleaners`.

## Add a cleaner

Example: a tool `foo` that caches to `~/.cache/foo`.

### 1. Write the class

Put it under `src/Cleaner.Core/Cleaners/`, in its ecosystem's folder (see
[architecture.md](architecture.md#cleaner-layout)). Get paths from `IEnvironmentService`; never
hardcode OS paths.

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

- If the tool has its own cleanup command, derive from `ProcessCleanerBase` and set `Executable` and
  `CleanArguments`. Declared paths then serve for sizing and for when the tool is not installed.
- For OS-specific or privileged cleaners, override `IsApplicable` (for example
  `context.Environment.IsWindows`) and `RequiresElevation`.
- If cleanup costs more than a re-download, set `ConfirmationWarning`.

### 2. Register it

Add a line to the matching `ServiceCollectionExtensions.*.cs` partial in
`src/Cleaner.Cli/Infrastructure/`:

```csharp
services.AddSingleton<ICleaner, FooCleaner>();
```

Then update the cleaner count in `tests/Cleaner.Core.Tests/CompositionRootTests.cs`.

### 3. Test and document it

- Add tests under `tests/Cleaner.Core.Tests/` with `TestContext.Create` and `FakeFileSystem`.
- `Safety/AllCleanersAgainstFakeHomeTests.cs` runs every registered cleaner against a realistic home
  folder. If your cleaner breaks it, fix the cleaner, not the test or the guard.
- Add a row to [cleaners.md](cleaners.md) and update the count there and in the README.

## Rules

- Target caches only: data the tool downloads or rebuilds by itself. Never installs, `bin` folders,
  version folders, or user data.
- Every deletion goes through `TargetRemover` and the deletion guard. Don't delete from a cleaner
  directly, and don't loosen the guard to make a target pass.
- Project scans use `ProjectRoots` and `BuildOutput`. Never match a folder by name alone.
- Keep it Native-AOT safe: no reflection or assembly scanning.
- Test Windows, macOS, and Linux branches.
