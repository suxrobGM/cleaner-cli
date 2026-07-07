# Changelog

All notable changes to **Cleaner** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/suxrobGM/cleaner-cli/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.4...v1.1.0
[1.0.4]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.3...v1.0.4
[1.0.3]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/suxrobGM/cleaner-cli/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.0.0
