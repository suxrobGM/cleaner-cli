# Changelog

All notable changes to **Cleaner** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.4] - 2026-06-16

### Fixed

- `build-artifacts` (and any cleaner deleting cache directories) no longer hangs on trees that
  contain symlinks or junctions — e.g. `node_modules` from pnpm/yarn workspaces. The read-only
  pre-walk that blocked deletion now skips reparse points and only runs on the rare delete that
  actually fails, so large sweeps complete quickly instead of stalling.
- More reliable CPU architecture detection in the Windows installer (`install.ps1`).

## [1.0.3] - 2026-06-16

### Added

- New cleaners: `ml-cache` (HuggingFace & Torch model caches), `vcpkg`, `spotify`,
  `konan` (Kotlin/Native), `azure-functions`, `dotslash`, and `unity` — the latter clears
  Unity's global editor cache plus the regenerable per-project `Library`/`Temp`/`Logs`/`obj`
  folders inside detected Unity projects. Adds the **Machine learning** and **Game
  development** categories.
- Repeatable `--path`/`-p` option so `build-artifacts` and `unity` can sweep several
  workspace roots in one run (e.g. `-p ~/source -p ~/work`).
- Broader `electron-app-cache` coverage: Claude, MongoDB Compass, Postman, Notion,
  Obsidian, Figma, Signal, and GitHub Desktop. `browser-automation` now also clears the
  `~/.cache` Playwright/Puppeteer locations, and the GPU shader cache cleaner clears the
  NVIDIA `NV_Cache` directory.

### Changed

- The `docker` cleaner now also prunes all unused build cache. With `--force` it
  additionally removes every unused image and named volume (which can delete data such as
  database volumes), gated behind the flag so a plain run stays safe.

## [1.0.2] - 2026-05-30

### Added

- Interactive mode now keeps the menu open after a clean run instead of exiting,
  returning to the cleaner selection until you choose to quit.

## [1.0.1] - 2026-05-30

### Added

- Install scripts (`install.ps1` / `install.sh`) that drop the binary into `~/.cleaner`.
- Resilient runs with Serilog file logging so failures are captured to a log.

### Fixed

- Use the cross `objcopy` when AOT-publishing `linux-arm64`.
- Resolve executables correctly on Windows and degrade gracefully on launch failure.

## [1.0.0] - 2026-05-30

### Added

- Initial release: cross-platform `cleaner` CLI built on .NET 10 + Native AOT.
- Cleaners for dev tooling, OS caches, and large apps, with scan → preview → confirm flow.
- `list`, `scan`, `clean`, `update`, and interactive menu commands.
- Self-update command with version reporting in the interactive banner.

[Unreleased]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.4...HEAD
[1.0.4]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.0.0
