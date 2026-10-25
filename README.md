<div align="center">

<img src="assets/icon.png" alt="Cleaner" width="120" height="120" />

# Cleaner

**Reclaim disk space from dev, OS, and app caches.**

[![CI](https://github.com/suxrobGM/cleaner-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/suxrobGM/cleaner-cli/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/suxrobGM/cleaner-cli?include_prereleases&sort=semver)](https://github.com/suxrobGM/cleaner-cli/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20macOS%20%7C%20Linux-informational)

</div>

---

Cleaner finds caches left by package managers, build tools, IDEs, browsers, the OS, and desktop
apps. It shows how much each one holds and deletes only what you approve.

- **132 cleaners** for dev tools, the operating system, and applications. See
  [docs/cleaners.md](docs/cleaners.md).
- **Preview first.** Every run scans, shows a size table, and asks before deleting.
- **Guarded.** Folders on PATH, installed tools, git repositories, and links are never deleted.
  Every removed path is logged.
- **One native binary** for Windows, macOS, and Linux. No .NET runtime needed.

## Install

The script puts the binary in `~/.cleaner/bin` and adds it to `PATH`.

macOS / Linux:

```bash
curl -fsSL https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.sh | bash
```

Windows (PowerShell):

```powershell
irm https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.ps1 | iex
```

Or download a binary from the [latest release](https://github.com/suxrobGM/cleaner-cli/releases)
and put `cleaner` on your `PATH`.

## Quick start

```bash
cleaner
```

```text
What would you like to do?
> Preview and clean caches
  List all cleaners
  Check for updates
  Exit
```

Pick **Preview and clean caches**, tick cleaners with the spacebar, and press Enter. Review the
size table, then confirm.

## Documentation

- [Usage](docs/usage.md): menu, options, the clean flow, safety, and logs.
- [Cleaners](docs/cleaners.md): every cleaner and what it removes.
- [Architecture](docs/architecture.md): how the code fits together.
- [Contributing](docs/contributing.md): build, test, and add a cleaner.

## License

[MIT](LICENSE)
