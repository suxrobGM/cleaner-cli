# Architecture

## Projects

| Project | Role |
| --- | --- |
| `src/Cleaner.Core` | Abstractions, all cleaners, and the services they use. No UI. |
| `src/Cleaner.Cli` | Native AOT executable: composition root, menu and flows, Spectre.Console rendering, System.CommandLine parsing. |
| `tests/Cleaner.Core.Tests` | xUnit tests against in-memory fakes (`FakeFileSystem`, `FakeEnvironment`, `FakeProcessRunner`). |

## Cleaner layout

```text
src/Cleaner.Core/Cleaners/
  Base/          base classes, TargetRemover, and shared helpers
  Applications/  desktop apps
  DevTools/      one folder per ecosystem (Python, Rust, JavaScript, Containers, Ides, ...)
  Os/            Windows, MacOs, Linux, CrossPlatform
```

Each area is one namespace (`Cleaner.Core.Cleaners.DevTools`, ...); subfolders add none. The menu
groups cleaners by their `Category` (see `Categories.cs`), not by folder.

## Cleaner model

Every cleaner implements `ICleaner`: `Id`, `Name`, `Category`, `RequiresElevation`,
`ConfirmationWarning`, `OptIn`, `IsApplicable`, `IsAvailable`, `ScanAsync`, and `CleanAsync`.

- `ScanAsync` returns `CleanupTarget` records (path, size, `DeleteMode`) and `Notes` for the preview.
- `CleanAsync` receives only the targets the user approved and removes nothing else.

Most cleaners derive from a base class:

| Base | Use |
| --- | --- |
| `DirectoryCleanerBase` | Declare cache paths in `GetTargets` (or `GetTargetsAsync`). The base checks existence and the guard, sizes targets, and removes them through `TargetRemover`. |
| `ProcessCleanerBase` | A tool command does the cleanup (`Executable`, `CleanArguments`, or `CommandSequence`). The command runs when the tool is on PATH; a failure is reported and nothing is deleted. Declared paths are used for sizing, and are deleted directly only when the tool is absent. Not offered in the folder picker. |

`DeleteMode` is `DeleteDirectory`, `ClearContents`, `DeleteFile`, or `ToolManaged` (a preview label
the tool reclaims; never deleted by Cleaner).

## Deletion safety

| Piece | Location | Job |
| --- | --- | --- |
| `IDeletionGuard` / `DeletionGuard` | `Services/Safety` | Decides if a path may be deleted. Built per run from `ProtectedPaths`, PATH (`IEnvironmentService.PathDirectories`), `~/.cleaner/exclude.txt`, and folders unticked in the picker. Rejects links and judges linked paths by their real location. Finds running programs via `IProcessRunner.RunningExecutablePaths`. Nothing can loosen it. |
| `ProtectedPaths` | `Services/Safety` | Essential system and profile folders, folders a project scan may not start in, and `Installs`: installed tools with the cache folders that may still be cleared. |
| `TargetRemover` | `Cleaners/Base` | Removes approved targets one by one: re-checks the guard, skips in-use folders, renames each folder aside, then deletes it. Records unfinished deletes in `~/.cleaner/pending-deletes.txt`. Returns every outcome in `CleanResult.Outcomes`. |
| `ProjectRoots`, `DirectorySearch`, `BuildOutput`, `GitIgnore` | `Cleaners/Base` | Project scans: refuse unsafe roots, walk without entering links, dot-folders, `node_modules`, or installs, and accept a folder only with proof it is build output. |
| `ToolHealthCheck` | `Services/Safety` | Before and after a clean, checks that common tools still run and PATH folders kept their files. |

Path rules are string logic in `PathComparison`, so Windows paths are tested correctly on Linux.
User-facing behavior is in [usage.md](usage.md#what-cleaner-never-deletes).

## Services

Cleaners get dependencies through `CleanupContext`, never static OS calls.

| Service | Role |
| --- | --- |
| `IEnvironmentService` | OS detection, standard paths, PATH folders, environment variables, elevation. |
| `IFileSystemService` | Sizing, enumeration (links not followed), link inspection, rename, delete. |
| `IProcessRunner` | PATH lookup, command execution with timeouts, running programs. |
| `ICleanerRegistry` | The registered cleaners, with lookup by id. |
| `IAppLogger` | Logging; `SerilogAppLogger` in the CLI writes `cleaner.log` and per-run logs. |

## CLI

- `CommandLineBuilder` parses `--path` and `--verbose` and the `update` subcommand, then starts the
  menu loop in `CleanerApp`.
- `CleanerApp.CleanFlow.cs` runs select, scan, preview, warnings, folder picker, confirm, clean, and
  summary. It builds the plan once and passes each cleaner only its approved targets.
- `CleanerRunner` wraps each scan and clean so one failing cleaner becomes a reported error, not a
  crash. Only cancellation propagates.
- `Program.cs` adds last-resort handlers so crashes are logged.

## Composition root and Native AOT

`ServiceCollectionExtensions.AddCleaner()` registers every service and cleaner explicitly, split
into partials by area (`.DevTools.cs`, `.Os.cs`, `.SystemPackageManagers.cs`, `.Applications.cs`).
There is no assembly scanning or reflection.

The CLI publishes with Native AOT (`PublishAot`, `InvariantGlobalization`). System.CommandLine is used
for parsing because Spectre.Console.Cli needs dynamic code; Spectre.Console is used only for
rendering. `IsAotCompatible` and `TreatWarningsAsErrors` make trim and AOT warnings fail the build.

## Distribution

Pushing a `vX.Y.Z` tag runs `.github/workflows/release.yml`, which publishes self-contained native
binaries for `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, and `linux-arm64` to GitHub
Releases.
