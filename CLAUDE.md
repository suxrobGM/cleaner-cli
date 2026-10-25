# CLAUDE.md

Guidance for Claude Code and contributors working in this repository.

## Project

Cleaner is an interactive, cross-platform CLI that reclaims disk space from dev tool, OS, and app
caches. Stack: .NET 10 with Native AOT, Spectre.Console (rendering only), System.CommandLine
(parsing; Spectre.Console.Cli needs reflection), and Microsoft.Extensions.DependencyInjection.

## Layout

- `src/Cleaner.Core/`: abstractions, cleaners, services, and the deletion guard.
- `src/Cleaner.Cli/`: Native AOT executable, menu and flows, rendering, composition root.
- `tests/Cleaner.Core.Tests/`: xUnit tests over in-memory fakes.
- `docs/`: [usage](docs/usage.md), [cleaners](docs/cleaners.md),
  [architecture](docs/architecture.md), [contributing](docs/contributing.md).

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/Cleaner.Cli -- --verbose
dotnet publish src/Cleaner.Cli -r win-x64 -c Release
```

Builds treat warnings as errors. Native AOT publishes must produce no trim or AOT warnings.

## Architecture

- Every cleaner implements `ICleaner`. File cleaners derive from `DirectoryCleanerBase`;
  command-driven ones from `ProcessCleanerBase`.
- Scan returns targets; clean receives only the approved targets and removes nothing else.
- `TargetRemover` performs every removal after `IDeletionGuard` (`Services/Safety/`) approves it.
- OS identity, standard paths, PATH, and elevation come from `IEnvironmentService`. Filesystem and
  process access go through `IFileSystemService` and `IProcessRunner`.
- Registrations are explicit, in `src/Cleaner.Cli/Infrastructure/ServiceCollectionExtensions.*.cs`.

## Adding a cleaner

1. Add the class under `src/Cleaner.Core/Cleaners/`, deriving from `DirectoryCleanerBase` or
   `ProcessCleanerBase`. Resolve paths through `IEnvironmentService`.
2. Register it in the matching `ServiceCollectionExtensions.*.cs` partial.
3. Add tests and update the count in `CompositionRootTests.cs`.
4. Document it in `docs/cleaners.md` and update the count there and in `README.md`.

Details: [docs/contributing.md](docs/contributing.md).

## Rules

- Native-AOT safe: no reflection, no assembly scanning.
- Interactive only. The menu drives cleanup; `update` is the only subcommand. No unattended
  deletion flags. Keep the scan, preview, confirm flow.
- Every deletion goes through `TargetRemover` and `IDeletionGuard`. Never call
  `IFileSystemService` delete methods from a cleaner, and never weaken the guard. Allow a cache
  folder in `ProtectedPaths.Installs` only when the tool re-downloads it by itself.
- Caches only: never an install, a `bin` folder, a version folder, or user data. Leave version
  pruning to the tool.
- Prefer the tool's own cleanup command. If it fails, report it; never fall back to deleting.
- Project scans use `ProjectRoots` and `BuildOutput`: a folder needs proof it is build output,
  never a name match alone.
- Set `ConfirmationWarning` when cleanup has a real trade-off.
- Keep the build and tests warning-free.
- Use focused conventional commits, such as `feat(cleaners): add Python cache cleaners`.
