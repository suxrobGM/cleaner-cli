# Changelog

All notable changes to **Cleaner** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.0.0
