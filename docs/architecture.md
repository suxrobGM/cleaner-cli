# Architecture

Cleaner is a small, layered .NET 10 application designed so new cleaners are cheap to add and the
whole thing compiles cleanly to Native AOT.

## Projects

- **`Cleaner.Core`** — a class library with the abstractions, all cleaners, and the services they
  depend on. UI-free and fully unit-testable.
- **`Cleaner.Cli`** — the Native AOT executable. Wires up dependency injection, owns the
  Spectre.Console rendering, and parses arguments with System.CommandLine.
- **`Cleaner.Core.Tests`** — xUnit tests that run cleaners against an in-memory filesystem fake.

## The cleaner model

Every cache target implements **`ICleaner`**:

```csharp
public interface ICleaner
{
    string Id { get; }
    string Name { get; }
    string Category { get; }
    bool RequiresElevation { get; }
    bool IsApplicable(CleanupContext context);  // right OS?
    bool IsAvailable(CleanupContext context);    // tool/paths present?
    Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken ct = default);
    Task<CleanResult> CleanAsync(CleanupContext context, IProgress<CleanProgress>? progress = null, CancellationToken ct = default);
}
```

Most cleaners don't implement this directly. They derive from a base class:

- **`DirectoryCleanerBase`** — declare candidate cache directories (`GetTargets`); the base handles
  existence checks, recursive sizing, dry-run accounting, deletion (whole-directory or
  clear-contents), and per-target error capture.
- **`ProcessCleanerBase`** — for tools where a native command is authoritative (e.g.
  `docker system prune`). Runs the command when the tool is on `PATH`, otherwise falls back to
  deleting the declared directories. Sizing always comes from those directories so scans and
  progress still report reclaimable space.

## Services

Everything a cleaner needs arrives through `CleanupContext`, never through static OS calls:

- **`IEnvironmentService`** — the single home of OS differences: path resolution, OS detection, and
  elevation (`IsElevated`). Cleaners ask it for `HomeDirectory`, `CacheDirectory`, etc., and report
  `IsApplicable` / `RequiresElevation` from it.
- **`IFileSystemService`** — best-effort sizing, enumeration, and deletion. Swapped for an in-memory
  fake in tests.
- **`IProcessRunner`** — `PATH` probing and process execution for `ProcessCleanerBase`.
- **`ICleanerRegistry`** — the explicitly-registered set of cleaners, with lookup by id and category.

## Composition root

`ServiceCollectionExtensions.AddCleaner()` registers everything with **explicit factory lambdas** —
no assembly scanning, no reflection-based activation. Cleaners have parameterless constructors and
receive their dependencies via `CleanupContext`, which keeps registration trivial and AOT-safe.

## Native AOT

The CLI targets Native AOT (`PublishAot=true`, `InvariantGlobalization=true`). Two deliberate
choices keep it warning-free:

1. **System.CommandLine** for argument parsing — it is trim/AOT-friendly. (Spectre.Console.Cli is
   not: its `CommandApp` is `[RequiresDynamicCode]`.) Spectre.Console is used only for rendering,
   which *is* AOT-safe.
2. **No reflection** in our own code — explicit registration everywhere.

`IsAotCompatible=true` runs the trim/AOT analyzers during every build, and `TreatWarningsAsErrors`
makes any regression fail CI. `dotnet publish -r <rid>` must produce zero trim/AOT warnings.

## Distribution

Distribution is **Native AOT binaries per RID** (GitHub Releases, built by `release.yml`). Each
release ships a self-contained native executable that needs no installed .NET runtime.
