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

Cleaner finds caches from package managers, build tools, IDEs, browsers, system components, and
apps, shows what can be reclaimed, and clears it only after confirmation.

- **131 built-in cleaners** across dev tools, the operating system, and applications.
- **Safe by default** — every run scans and previews first; nothing is deleted without confirmation,
  and the few cleaners with a real trade-off ask again on their own.
- **Cross-platform** — a single native binary for Windows, macOS, and Linux (no runtime required).
- **Interactive by design** — choose, preview, and confirm from one menu; no unattended flags.

## What it cleans

- **Developer tools:** NuGet, npm/yarn/pnpm/bun, pip/poetry/uv, Cargo, Go, Gradle/Maven, Conan,
  Composer, Julia, Zig, version managers, IDEs, build outputs, and more.
- **Containers and infrastructure:** Docker, Podman, Helm, minikube, Pulumi, and related caches.
- **Applications:** browsers, messaging apps, game launchers, and data left by uninstalled apps.
- **Operating systems:** temp files, trash, package-manager caches, Windows Update, WinSxS, GPU
  installer leftovers, and `Windows.old`.

See the full list in **[docs/cleaners.md](docs/cleaners.md)**.

## Install

The install script downloads the platform binary to `~/.cleaner/bin` and adds it to `PATH`.

**macOS / Linux**

```bash
curl -fsSL https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.sh | bash
```

**Windows** (PowerShell)

```powershell
irm https://raw.githubusercontent.com/suxrobGM/cleaner-cli/main/scripts/install.ps1 | iex
```

Or **download a binary** manually from the [latest release](https://github.com/suxrobGM/cleaner-cli/releases),
unpack it, and put `cleaner` on your `PATH`.

## Update

Pick **Check for updates** in the menu to download, replace, and relaunch the platform binary.
`cleaner --version` prints the installed version without touching the network.

## Quick start

Run it:

```bash
cleaner
```

```text
What would you like to do?
> Clean caches
  Preview only (nothing is deleted)
  List all cleaners
  Check for updates
  Exit
```

Pick **Clean caches**, select entries with the spacebar (toggle **All cleaners** for everything), and
press Enter. The `--path` flag points project-local cleaners at your code; `--verbose` shows a
per-directory breakdown.

A run looks like this:

```text
╭──────────────┬─────────────╮
│ Cleaner      │ Reclaimable │
├──────────────┼─────────────┤
│ NuGet caches │       22 GB │
│ Gradle caches│      3.1 GB │
│ Total        │      25 GB  │
╰──────────────┴─────────────╯
Delete 25 GB across 2 cleaner(s)? [y/N]
```

## Documentation

- **[Usage](docs/usage.md)** — the menu, the two flags, and what each action does.
- **[Cleaners](docs/cleaners.md)** — the full catalog and what each one removes.
- **[Architecture](docs/architecture.md)** — how it's built.
- **[Contributing](docs/contributing.md)** — add a new cleaner in a few lines.

## License

[MIT](LICENSE)
