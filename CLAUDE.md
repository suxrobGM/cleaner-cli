# CLAUDE.md

Guidance for Claude Code and contributors working in this repository.

## Project

Cleaner is an interactive, cross-platform CLI for reclaiming disk space from development tools,
operating systems, and application caches. It uses .NET 10 with Native AOT, Spectre.Console for the
UI, System.CommandLine for argument parsing, and Microsoft.Extensions.DependencyInjection.

System.CommandLine is used instead of Spectre.Console.Cli because the latter relies on reflection
and dynamic code. Spectre.Console is used only for AOT-safe rendering.

## Layout

- `src/Cleaner.Core/` — abstractions, cleaner implementations, and testable services.
- `src/Cleaner.Cli/` — Native AOT executable, interactive flows, rendering, and composition root.
- `tests/Cleaner.Core.Tests/` — xUnit tests using in-memory fakes where possible.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/Cleaner.Cli
dotnet run --project src/Cleaner.Cli -- --verbose
dotnet publish src/Cleaner.Cli -r win-x64 -c Release
```

Builds treat warnings as errors. Native AOT publishes must produce no trim or AOT warnings.

## Architecture

- Every cleaner implements `ICleaner`.
- Filesystem cleaners usually derive from `DirectoryCleanerBase`, which handles sizing, dry runs,
  deletion, progress, and error capture.
- Command-driven cleaners usually derive from `ProcessCleanerBase`.
- Platform identity, standard paths, and elevation checks belong in `IEnvironmentService`.
- Filesystem and process access go through `IFileSystemService` and `IProcessRunner` for testability.
- Cleaner registrations are explicit to remain Native-AOT compatible.

## Adding a cleaner

1. Add the cleaner under `src/Cleaner.Core/Cleaners/`, normally deriving from
   `DirectoryCleanerBase` or `ProcessCleanerBase`.
2. Resolve paths through `IEnvironmentService`; do not hardcode platform-dependent locations.
3. Register the cleaner in the matching `ServiceCollectionExtensions.*.cs` partial under
   `src/Cleaner.Cli/Infrastructure/`.
4. Add tests and document the cleaner in `docs/cleaners.md`.

## Conventions

- Keep code Native-AOT safe: avoid reflection-based discovery and assembly scanning.
- Keep deletion interactive. The main menu drives cleanup; `update` is the only direct subcommand.
  Do not add unattended deletion flags.
- Preserve the scan → preview → confirm flow. Set `ConfirmationWarning` when cleanup has a material
  trade-off, and never delete user data.
- Keep the build and tests clean; `TreatWarningsAsErrors` and AOT analyzers are enabled.
- Prefer focused conventional commits, such as `feat(cleaners): add Python cache cleaners`.
