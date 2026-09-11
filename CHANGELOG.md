# Changelog

All notable changes to **Cleaner** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.4] - 2026-09-10

### Added

- Folders can be picked individually before deleting. A sweep over a source tree can match hundreds
  of them, and taking all of them means reinstalling dependencies for every project still in use.
  After the size table, a cleaner that found more than one folder can be opened up and its folders
  listed by name with their sizes. Everything starts ticked, so untick only what to keep. Declining
  leaves the run unchanged. Command-driven cleaners are not offered, since the tool decides.

## [1.2.3] - 2026-09-10

### Fixed

- A scan no longer stalls on `winsxs`. Sizing the component store means running
  `DISM /AnalyzeComponentStore`, which walks every installed component and takes minutes, or blocks
  indefinitely while Windows servicing holds its lock — the scan waited on it with no deadline. The
  scan now skips it and the cleaner reports its size after running, as the other command-driven
  cleaners do. DISM is still measured either side of the cleanup, so the freed figure is unchanged.
- Every DISM call now carries a deadline, so a wedged servicing stack fails `winsxs` with a message
  instead of hanging the run. Command-driven cleaners can set their own deadline the same way.
- `winsxs` now asks for its own confirmation, because it runs for many minutes with no progress of
  its own.

## [1.2.2] - 2026-09-10

### Changed

- The interactive menu now has one cache action: it always scans and previews the selected caches,
  then asks for confirmation before deleting anything.

### Fixed

- Docker usage scans stop waiting after 15 seconds when an installed CLI cannot reach its daemon,
  and cancelled child processes are terminated instead of being left behind. The final pending
  cleaner names are shown in the scan status to make slow scans identifiable.

### Performance

- Executable lookups on PATH are resolved once per session instead of on every availability check,
  removing roughly a thousand file probes per absent tool per check.

## [1.2.1] - 2026-09-10

### Added

- The cleaner list is grouped into three areas — operating system, development, and application —
  so a long list is easier to scan.
- Docker, its WSL2 virtual disk, and the Windows component store (WinSxS) now report how much they
  hold before you run them, instead of only reporting what they freed afterwards.

### Changed

- Cleanups reuse the size measured during the scan as their baseline, so a clean no longer re-walks
  directories it has already sized.

### Fixed

- The cleaner list stays on screen instead of scrolling away as soon as it is printed.
- `windows-installer-orphans` normalizes cached package paths before comparing them, so packages
  that are still referenced are no longer treated as orphans because of path casing or separators.

## [1.2.0] - 2026-09-10

### Added

- **11 new cleaners**, aimed at the caches that actually dominate a loaded dev machine:
  - `vscode-cpptools` — the C/C++ extension's IntelliSense store (`ipch` plus a symbol database
    per workspace); usually the largest cache VS Code produces.
  - `android-studio` — uses the JetBrains layout under a `Google` root, so `jetbrains` never saw
    it, and every upgrade leaves the previous version's caches behind.
  - `amd-telemetry` — `ProgramData\AMD\PPC` logs are append-only and never rotated, so
    `sdkusage.csv` alone reaches several GB.
  - `winre-agent` — the `C:\$WinREAgent` scratch folder Windows Setup leaves behind.
  - `razer` — Cortex caches plus `CortexFPSData.db3`, an FPS history that is never pruned.
  - `claude-desktop` — the local-agent VM rootfs, re-downloaded on demand.
  - `codex` — `~/.codex` scratch and rotated sandbox logs; sessions and auth are kept.
  - `docker-vhdx` — compacts Docker Desktop's WSL2 virtual disks. Pruning frees space *inside*
    the disk; the host `.vhdx` only ever grows. Shuts WSL down, then compacts with `diskpart`.
    Deletes nothing, and aborts rather than compact an attached disk.
  - `ngen-cache` — the .NET Framework native image cache, never the GAC.
  - `windows-installer-orphans` — cached `.msi`/`.msp` packages that no installed product or patch
    references. The live set is read from the Installer's `UserData` registry key in one
    `reg query`; if that read fails or comes back empty the cleaner does nothing, rather than
    treat the whole cache as garbage.
  - `app-leftovers` — see below.
- `build-artifacts` now sweeps Python virtualenvs (`.venv`, `venv`, `.tox`, `.nox`) and additional
  frontend caches (`.turbo`, `.parcel-cache`, `.vite`). It also takes `build`, but only beside a
  Gradle, Maven, CMake, or Meson project file—the name is too common to sweep on sight.
- Extended: `gpu-installers` covers the NVIDIA app's update staging and the NGX (DLSS) model store;
  `browser-cache` the on-device AI model stores Chrome and Edge keep beside their profiles;
  `vscode` its `WebStorage` and `Crashpad` directories; `browser-automation` the Playwright MCP
  profile directory.
- `DeleteMode.DeleteFile`, for cleaners whose target is a single large file rather than a directory,
  and `IFileSystemService.WriteAllText`, used by `docker-vhdx` for its `diskpart` script.

- **`app-leftovers`** — removes the per-user profile directories that uninstalled applications leave
  behind. Uninstallers routinely drop the program but keep its data, which for an Electron app that
  bundles a runtime or a VM image runs to gigabytes. Seeded with Claude Desktop, Docker Desktop
  (including its WSL2 virtual disk), Discord, Slack, Unity Hub, and the Epic Games Launcher, on
  Windows and macOS. Every app is listed explicitly as a pair of install markers and data
  directories, and leftovers are offered only when all of that app's markers are gone — a
  name-matching heuristic over `AppData` flags live tools like nvm and vcpkg, so the list is curated
  rather than inferred. Because it removes settings and history rather than cache, it sets
  `ConfirmationWarning` and is confirmed on its own.

### Changed

- **Cleaner is now interactive only.** Running `cleaner` opens a menu — Clean caches, Preview only,
  List all cleaners, Check for updates, Exit — and every action is chosen, previewed, and confirmed
  there. The `list`, `scan`, and `clean` subcommands are gone; `update` remains (it may be needed
  before the menu is useful) and is also reachable from the menu.
- **`--force` is gone, and its behavior is now the default.** It was redundant with the confirmation
  prompt and did not gate what it claimed to. `docker` and `podman` now always run
  `system prune -a --volumes` (plus `docker builder prune -a`), and `conan` always follows
  `cache clean "*"` with `conan remove "*"`.
- **`windows-old` is no longer flag-gated.** It appears in the menu like any other cleaner and states
  its trade-off — deleting it gives up Windows upgrade rollback — in its own yes/no prompt just
  before the run-wide confirmation. Declining it drops only that cleaner.
- `ICleaner.RequiresForce` is replaced by `ICleaner.ConfirmationWarning`, a string explaining the
  trade-off; a non-null value triggers the per-cleaner prompt. `CleanupContext.Force` is removed.

### Removed

- The `--yes` and `--json` flags, the `scan --json` report, and the `list` / `scan` / `clean`
  subcommands. There is no longer any way to delete without a human at the prompt. Use the menu's
  **Preview only** action in place of `scan` / `--dry-run`.

## [1.1.1] - 2026-07-07

### Changed

- Internal code reorganization only — no behavior change and no change to the cleaner catalog
  (still 120 cleaners). The multi-class cleaner "bundle" files were split into one file per
  cleaner and grouped into per-tool folders, the DI composition root was split into
  per-category registration methods, and several CLI helpers (`FileSystemService`,
  `ConsoleRenderer`, `CleanerApp`) were simplified. The full test suite still passes.

## [1.1.0] - 2026-07-07

### Added

- **45 new cleaners** across dev tools, apps, and Windows:
  - Languages & package managers: `conan` (force-gated package removal), `zig`, `swiftpm`,
    `opam`, `cpanm`, `julia` (compiled/logs only), `rubygems`, `renv`, `luarocks`, `nim`,
    `texlive`.
  - Containers / IaC: `podman`, `helm`, `minikube`, `vagrant` (tmp only), `pulumi` plugins,
    `kubectl` caches, `ansible`, `lima`.
  - Python: `pipx` (cache/logs, never venvs), `pre-commit`.
  - Tooling downloads: `corepack`, `nvm`, `mise`, `asdf`, `sdkman`, `node-gyp`, `gcloud`
    logs, `sonar` cache.
  - Machine learning: `wandb`; `ml-cache` now also covers Keras datasets and kagglehub.
  - Apps: `telegram` media cache (never account state), `game-launchers`
    (Epic/Battle.net/GOG/EA/Riot web caches), `adobe-media-cache`, `onedrive` logs,
    `dropbox` internal cache, `cocoapods`; WhatsApp/Element and new Teams WebView2 caches
    in `electron-app-cache`.
  - Game development: `unreal` DerivedDataCache.
  - IDEs: `zed`, `neovim`; `vscode` now covers Cursor/VSCodium/Windsurf; `xcode` adds
    DeviceSupport and simulator caches.
  - OS / package managers: `winget` downloads+logs, `flatpak` unused runtimes, `nix`
    garbage collection, `gpu-installers` (extraction leftovers only), `winsxs` (DISM
    component cleanup), `windows-old` (force-gated).
- **Force-gated cleaners** (`ICleaner.RequiresForce`): cleanups with a real trade-off beyond
  re-fetching a cache (e.g. removing Windows.old drops upgrade rollback) stay scannable but
  are skipped — with an explicit message — unless `--force` is passed.
- **Parallel scanning**: scans run concurrently (bounded by CPU count, max 8) with a live
  scanned-count status, cutting wall-clock time on dozens of disk walks.
- **`scan --json`** emits a machine-readable report via a source-generated
  `JsonSerializerContext` (AOT-safe), making the scripts-and-CI story real.
- **`--verbose`** on `clean`/`scan` lists the individual directories behind each size.

### Changed

- Non-TTY runs skip the figlet banner, spinners, progress bars, and prompts; the interactive
  menu and unconfirmed cleans point at `scan`/`clean --yes`.
- Command-based cleaners (docker, dnf, …) show `n/a (runs command)` instead of being hidden
  as 0 B, and dry runs note that they are not in the estimate
  (new `ICleaner.SupportsSizeEstimate`).
- Unknown `--category` names now list the valid categories instead of silently matching
  nothing, and interactive selection is keyed by cleaner id so duplicate display names cannot
  mismap.

### Fixed

- `Ctrl+C` is now treated as cancellation: it prints a plain `Cancelled.` message and exits
  with the conventional SIGINT code 130 instead of reporting an unexpected error.
- Cleaners honor cache-relocation environment variables (`NUGET_PACKAGES`, `CARGO_HOME`,
  `RUSTUP_HOME`, `GOMODCACHE`, `GRADLE_USER_HOME`, `npm_config_cache`, `YARN_CACHE_FOLDER`,
  `BUN_INSTALL(_CACHE_DIR)`, `PIP_CACHE_DIR`, `POETRY_CACHE_DIR`, `UV_CACHE_DIR`, `PUB_CACHE`),
  so scans no longer under-report and deletion no longer misses relocated caches.
- `conda` and `yarn` cache locations corrected: conda now finds the package cache under the
  install root (`<root>/pkgs`) and honors `CONDA_PKGS_DIRS`/`CONDA_PREFIX`/`MAMBA_ROOT_PREFIX`;
  yarn covers the capital-Y macOS Classic cache and the Berry global cache. Target de-dup is
  now platform-aware (case-sensitive on Linux) and checks existence before consuming a slot.
- The `jetbrains` cleaner no longer wipes Toolbox-installed IDEs on Windows — it now targets
  only the caches, index, log, and tmp subdirectories of each product and skips Toolbox.

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

[Unreleased]: https://github.com/suxrobGM/cleaner-cli/compare/v1.2.4...HEAD
[1.2.4]: https://github.com/suxrobGM/cleaner-cli/compare/v1.2.3...v1.2.4
[1.2.3]: https://github.com/suxrobGM/cleaner-cli/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/suxrobGM/cleaner-cli/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/suxrobGM/cleaner-cli/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.4...v1.1.0
[1.0.4]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.0.0
