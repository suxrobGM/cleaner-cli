# Architecture

Cleaner is a layered .NET 10 application designed to be easy to extend and compatible with Native
AOT.

## Projects

- **`Cleaner.Core`** — a class library with the abstractions, all cleaners, and the services they
  depend on. UI-free and fully unit-testable.
- **`Cleaner.Cli`** — the Native AOT executable. Wires up dependency injection, owns the
  Spectre.Console rendering, and parses the handful of flags with System.CommandLine.
- **`Cleaner.Core.Tests`** — xUnit tests that run cleaners against an in-memory filesystem fake.

## Where cleaners live

`Cleaner.Core/Cleaners` is split into three areas, each one namespace: `Applications` for desktop
apps, `DevTools` for developer tooling, and `Os` for the operating system. Folders below an area
group by domain and do not add namespace segments, so moving a cleaner between them is a file move
and nothing else.

```
Cleaners/
  Base/                 DirectoryCleanerBase, ProcessCleanerBase, and the shared path helpers
  Applications/         Steam, Spotify, Electron apps, uninstalled-app leftovers
  DevTools/             one folder per ecosystem: Python, Rust, JavaScript, Jvm, Cpp, Ruby, …
                        plus Containers, Kubernetes, Infrastructure, Ides, BuildCaches,
                        VersionManagers, ToolingDownloads, MachineLearning, Mobile, GameDev
  Os/                   Windows, MacOs, Linux, and CrossPlatform
```

`Languages` is the home for ecosystems with a single cleaner; once a second one arrives it earns its
own folder. The folder a cleaner sits in is for people reading the code — the `Category` it declares
is what groups it in the menu, and the two are deliberately independent.

## The cleaner model

Every cache target implements **`ICleaner`**:

```csharp
public interface ICleaner
{
    string Id { get; }
    string Name { get; }
    string Category { get; }
    bool RequiresElevation { get; }
    bool SupportsSizeEstimate { get; }
    bool SupportsPartialSelection { get; }
    string? ConfirmationWarning { get; }
    bool OptIn { get; }                         // left out of "All cleaners"
    bool IsApplicable(CleanupContext context);  // right OS?
    bool IsAvailable(CleanupContext context);   // tool or unprotected paths present?
    Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken ct = default);
    Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,  // exactly what the user reviewed
        IProgress<CleanProgress>? progress = null,
        CancellationToken ct = default);
}
```

A scan returns `CleanupTarget(Path, Bytes, Mode, Description)` records plus `Notes` (protected paths
left alone, refused sweep roots). The app shows them, the user approves or unticks them, and each
cleaner's `CleanAsync` receives only its approved targets. It never looks for targets again, so the
run can't reach past the preview.

Most cleaners derive from a base class:

- **`DirectoryCleanerBase`** — declare candidate cache directories (`GetTargets`, or
  `GetTargetsAsync` when discovery itself needs a registry read or an external command); the base
  handles existence checks, the guard, recursive sizing, and dry-run accounting, and hands removal to
  `TargetRemover` (whole-directory, clear-contents, or single file).
- **`ProcessCleanerBase`** — for tools where a native command is authoritative (e.g.
  `npm cache clean`, `docker system prune`). Runs the command whenever the tool is on `PATH`; a failed
  command is reported and nothing is deleted in its place. Only when the tool is absent are the
  approved directories removed directly. Specialized cleaners can override sizing with command output.

## Deletion safety

Three pieces stand between a cleaner and the disk:

- **`IDeletionGuard`** (`Services/Safety`) decides whether a path may go. It is built per run by
  `DeletionGuard.Create` from `ProtectedPaths` (essential folders, installed-tool roots with their
  cache holes), the PATH (`IEnvironmentService.PathDirectories`: process, user, and machine), the
  user's `~/.cleaner/exclude.txt`, and folders the user unticked this run. It refuses links, judges a
  path reached through a link by its real location, and finds running programs through
  `IProcessRunner.RunningExecutablePaths`. Path rules are plain string logic (`PathComparison`), so
  Windows paths are checked correctly on a Linux test runner. Nothing can loosen it.
- **`TargetRemover`** (`Cleaners/Base`) removes approved targets one at a time. It re-checks the guard,
  skips anything in use, renames each folder aside in its own parent, then deletes it. The rename is
  atomic, and on Windows it fails while any file inside is open, so a folder is removed whole or not
  at all. Under clear-contents, each child is checked and staged on its own. Every removal, skip, and
  failure is written to the run log and returned as `CleanResult.Outcomes`.
- **`ProjectRoots` and `BuildOutput`** (`Cleaners/Base`) keep workspace sweeps inside projects.
  A sweep refuses roots the guard calls unusable (home, drive roots, system and install folders),
  never descends into links, dot-folders, `node_modules`, or installed tools, and takes a folder only
  when its project proves it is build output.

`ToolHealthCheck` runs before and after each clean. It reports any common tool whose version command
stopped working and any PATH folder that lost files.

## Services

Cleaners receive dependencies through `CleanupContext`, not static OS calls:

- **`IEnvironmentService`** — the single home of OS differences: path resolution, OS detection, and
  elevation (`IsElevated`). Cleaners ask it for `HomeDirectory`, `CacheDirectory`, etc., and report
  `IsApplicable` / `RequiresElevation` from it.
- **`IFileSystemService`** — best-effort sizing, enumeration (links are never followed), link
  inspection, rename, and deletion. Swapped for an in-memory fake in tests.
- **`IProcessRunner`** — `PATH` probing and process execution for `ProcessCleanerBase`.
- **`ICleanerRegistry`** — the explicitly-registered set of cleaners, with lookup by id and category.

## The user interface

Cleaner is interactive by default and has no unattended deletion mode. `CommandLineBuilder` parses
`--path`/`--verbose` (plus `--help`/`--version`) before opening the `MainMenuChoice` loop. The
`update` action is also available as a direct subcommand. Every deletion requires confirmation;
without an interactive terminal, the app prints guidance and exits 1.

Two prompts guard a run. The run-wide one names the total and the cleaner count. Before it, any
cleaner exposing a non-null `ICleaner.ConfirmationWarning` prints that trade-off and takes its own
yes/no, so declining one (say `windows-old`) drops just that cleaner and leaves the rest of the run
intact.

## Composition root

`ServiceCollectionExtensions.AddCleaner()` uses explicit registrations—no assembly scanning or
reflection-based activation. Cleaners receive runtime dependencies through `CleanupContext`,
keeping registration simple and AOT-safe.

## Native AOT

The CLI targets Native AOT (`PublishAot=true`, `InvariantGlobalization=true`). Two deliberate
choices keep it warning-free:

1. **System.CommandLine** for argument parsing — it is trim/AOT-friendly. (Spectre.Console.Cli is
   not: its `CommandApp` is `[RequiresDynamicCode]`.) Spectre.Console is used only for rendering,
   which *is* AOT-safe.
2. **No reflection** in our own code — explicit registration everywhere.

`IsAotCompatible=true` runs trim/AOT analyzers during every build, and `TreatWarningsAsErrors` makes
regressions fail CI. `dotnet publish -r <rid>` must produce zero trim/AOT warnings.

## Resilience & logging

`CleanerRunner` wraps every per-cleaner scan/clean: an unexpected exception becomes a reported error
plus a log entry instead of aborting the run (only cancellation propagates). Logging goes through the
`IAppLogger` abstraction (Core), implemented in the CLI by `SerilogAppLogger` — a code-only, AOT-safe
Serilog config writing `~/.cleaner/logs/cleaner.log`, plus one `runs/run-<timestamp>.log` per clean
with the approved plan and every path removed or skipped (`IAppLogger.BeginRun` / `Detail`).
`Program.cs` adds last-resort
`AppDomain`/`TaskScheduler` handlers so even a crash is logged.

## Distribution

Distribution is **Native AOT binaries per RID** (GitHub Releases, built by `release.yml`). Each
release ships a self-contained native executable that needs no installed .NET runtime.
