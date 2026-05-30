# CLAUDE.md

Guidance for Claude Code (and humans) working in this repository.

## What this is

**Cleaner** is a cross-platform CLI (command: `cleaner`) that reclaims disk space by clearing
caches from dev tooling (NuGet, npm/yarn/pnpm/bun, pip, cargo, go, Gradle/Maven, etc.), the OS
(temp/trash, Windows Update cache, browser caches, system package managers), and large apps
(Steam). Built on **.NET 10 + Native AOT**, **Spectre.Console** for rich UI, **System.CommandLine**
for AOT-safe argument parsing, and **Microsoft.Extensions.DependencyInjection** for DI.

> **Why System.CommandLine, not Spectre.Console.Cli?** Spectre.Console.Cli relies on reflection
> (`CommandApp` is `[RequiresDynamicCode]`) and cannot be Native-AOT-compiled cleanly.
> System.CommandLine is trim/AOT-friendly (the dotnet CLI uses it). We use Spectre.Console purely
> for rendering (tables, prompts, progress, figlet) — that part *is* fully AOT-safe.

## Project layout

- `src/Cleaner.Core/` — class library: abstractions, cleaner implementations, services. Unit-testable.
- `src/Cleaner.Cli/` — Native AOT executable: Spectre.Console.Cli commands + DI composition root.
- `tests/Cleaner.Core.Tests/` — xUnit tests against an in-memory filesystem fake.

## Commands

```bash
dotnet build                                   # build all (warnings are errors)
dotnet test                                    # run unit tests
dotnet run --project src/Cleaner.Cli           # interactive menu
dotnet run --project src/Cleaner.Cli -- list   # list cleaners
dotnet run --project src/Cleaner.Cli -- clean nuget --dry-run
dotnet publish src/Cleaner.Cli -r win-x64 -c Release   # Native AOT binary (must be 0 trim warnings)
```

## Architecture in brief

- Every cleaner implements `ICleaner` (`Id`, `Name`, `Category`, `RequiresElevation`,
  `IsApplicable`, `IsAvailable`, `ScanAsync`, `CleanAsync`).
- Most cleaners derive from **`DirectoryCleanerBase`** — declare candidate cache directories, the
  base handles size calc, dry-run accounting, deletion, and error capture.
- Cleaners that must shell out (e.g. `docker system prune`) derive from **`ProcessCleanerBase`**.
- OS differences live **only** in `IEnvironmentService` (paths, OS detection, elevation). Cleaners
  never hardcode OS paths or branch on the platform directly.
- All filesystem access goes through `IFileSystemService` so cleaners are testable.

## Adding a new cleaner

1. Create a class in `src/Cleaner.Core/Cleaners/` deriving from `DirectoryCleanerBase`
   (or `ProcessCleanerBase`). Resolve paths via the injected `IEnvironmentService`.
2. Register it with one line in the composition root
   (`src/Cleaner.Cli/Infrastructure/ServiceCollectionExtensions.cs`):
   `services.AddSingleton<ICleaner, XCleaner>();`
3. Document it in `docs/cleaners.md`.

## Rules / conventions

- **Native AOT safe**: no reflection-based discovery, no assembly scanning. Register cleaners
  **explicitly** in the composition root. `IsAotCompatible=true` runs the trim/AOT analyzers at
  build, and `dotnet publish -r <rid>` must produce **zero trim/AOT warnings**.
- **Safe by default**: deletes are gated behind scan → preview → confirm (or `--yes`). Never delete
  user data — only caches/temp/derived artifacts.
- **Commit by group**: land focused, conventional commits (e.g.
  `feat(cleaners): add Python cache cleaners`), not one big commit.
- `TreatWarningsAsErrors` is on; keep the build clean.
