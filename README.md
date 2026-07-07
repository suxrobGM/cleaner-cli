<div align="center">

<img src="assets/icon.png" alt="Cleaner" width="120" height="120" />

# Cleaner

**Reclaim disk space from dev, OS, and app caches — one fast, safe command.**

[![CI](https://github.com/suxrobGM/cleaner-cli/actions/workflows/ci.yml/badge.svg)](https://github.com/suxrobGM/cleaner-cli/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/suxrobGM/cleaner-cli?include_prereleases&sort=semver)](https://github.com/suxrobGM/cleaner-cli/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20macOS%20%7C%20Linux-informational)

</div>

---

Cleaner finds the caches that quietly eat your disk — package managers, build tools, IDEs,
browsers, system junk, even Steam — shows you exactly how much you'd get back, and clears them
only after you say yes.

- **120 built-in cleaners** across dev tools, the operating system, and applications.
- **Safe by default** — every run scans and previews first; nothing is deleted without confirmation.
- **Cross-platform** — a single native binary for Windows, macOS, and Linux (no runtime required).
- **Two ways to use it** — a friendly interactive menu, or direct commands (with `scan --json`) for
  scripts and CI.

## What it cleans

Package managers & languages (NuGet, npm/yarn/pnpm/bun, pip/poetry/uv, Cargo, Go, Gradle/Maven,
Conan, Composer, pub, Julia, Zig, and more), build & monorepo caches (ccache, Bazel, Turbo/Nx),
containers & IaC (Docker, Podman, Helm, minikube, Pulumi), IDEs (JetBrains, VS Code/Cursor, Visual
Studio, Xcode, Unreal/Unity derived data), version managers (mise, asdf, nvm, SDKMAN!), browsers,
messaging apps (Discord, Telegram, WhatsApp, Teams), game launchers (Steam, Epic, Battle.net, GOG,
EA, Riot), OS junk (temp, recycle bin, Windows Update cache, WinSxS, GPU driver installer
leftovers, `Windows.old`), and system package managers (apt/dnf/pacman/brew/scoop/choco/winget/
flatpak/nix).

See the full list in **[docs/cleaners.md](docs/cleaners.md)**.

## Install

The install script grabs the right binary for your platform, drops it in `~/.cleaner/bin`, and adds
that to your `PATH`.

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

Check your version and update in place — Cleaner downloads the right binary for your platform,
replaces itself, and relaunches:

```bash
cleaner --version          # show the installed version
cleaner update --check     # is a newer release available?
cleaner update             # download & install it
```

## Quick start

```bash
# Launch the interactive menu — pick what to clean, preview, confirm
cleaner

# See everything Cleaner can clean and how much space is reclaimable
cleaner list

# Preview without deleting
cleaner clean nuget npm --dry-run

# Clean specific tools, skipping the prompt
cleaner clean gradle docker --yes

# Clean a whole category, or everything
cleaner clean --category "Package managers"
cleaner clean --all
```

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

- **[Usage](docs/usage.md)** — every command, flag, and example.
- **[Cleaners](docs/cleaners.md)** — the full catalog and what each one removes.
- **[Architecture](docs/architecture.md)** — how it's built.
- **[Contributing](docs/contributing.md)** — add a new cleaner in a few lines.

## License

[MIT](LICENSE)
